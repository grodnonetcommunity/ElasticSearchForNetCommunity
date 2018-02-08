using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest;

namespace ElasticSearchForNetCommunity
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Using InMemoryConnection allows to mock responses - easy for unit tests
            // var connection = new InMemoryConnection();
            var settings = new ConnectionSettings(new SingleNodeConnectionPool(new Uri("http://localhost:9200")));
            settings.EnableDebugMode();
            var client = new ElasticClient(settings);
            var indexName = "testindex";
            var indexDescriptor = new CreateIndexDescriptor(indexName)
                .Mappings(ms => ms
                    .Map<IndexModel>(m => m.AutoMap()))
                .Settings(s =>
                {
                    s = s.Setting("index.max_result_window", 50_000);
                    return s.NumberOfShards(4);
                });

            if ((await client.IndexExistsAsync(indexName)).Exists)
            {
                await client.DeleteIndexAsync(indexName);
            }

            var createIndexResponse = await client.CreateIndexAsync(indexDescriptor);
            var topLeftCoordinate = new GeoCoordinate(47.435674, -123.719505);
            var bottomRightCoordinate = new GeoCoordinate(33.119396, -81.339195);
            HandleErrors(createIndexResponse);

            var models = new List<IndexModel>();
            var random = new Random();
            for (int i = 0; i < 10_000; i++)
            {
                var newModel = new IndexModel
                {
                    Id = Guid.NewGuid(),
                    Boolean = random.NextDouble() >= 0.5,
                    Number = random.Next(),
                    Text = "Test Text",
                    Position = new GeoCoordinate(
                        random.NextDouble(topLeftCoordinate.Latitude, bottomRightCoordinate.Latitude),
                        random.NextDouble(topLeftCoordinate.Longitude, bottomRightCoordinate.Longitude)),
                    ScaledFloat = (float) random.NextDouble()
                };
                models.Add(newModel);
            }

            var response = await client.IndexManyAsync(models, indexName);
            HandleErrors(response);

            var searchResult =
                await client.SearchAsync<IndexModel>(sr => sr.Index(indexName).Size(0)
                    .Query(
                        qc =>
                            qc.Bool(bc => bc.Must(
                                q => q.GeoBoundingBox(t =>
                                    t.Field(e => e.Position).BoundingBox(new GeoLocation(47.435674, -123.719505),
                                        new GeoLocation(40.277535, -102.52935)))
                            ))).Aggregations(a => a.GeoHash("aggregated_points",
                        ghg => ghg.Field(e => e.Position).GeoHashPrecision(GeoHashPrecision.Precision11)
                            .Size(int.MaxValue)
                            .Aggregations(a1 => a1.Sum("sum_number", sd => sd.Field(e => e.Number))))));
            HandleErrors(searchResult);
            foreach (var document in searchResult.Documents)
            {
                Console.WriteLine(document.ToString());
            }

            var scrollId = searchResult.ScrollId;
            while (searchResult.Documents.Any())
            {
                searchResult = await client.ScrollAsync<IndexModel>("1m", scrollId);
                scrollId = searchResult.ScrollId;
            }

            Console.WriteLine("Press any key to continues...");
            Console.ReadKey();
        }

        static void HandleErrors(IResponse response)
        {
            if (!response.IsValid)
            {
                if (response.OriginalException != null)
                {
                    Console.WriteLine(response.OriginalException.Message);
                }
                else if (response.ServerError != null)
                {
                    Console.WriteLine(response.ServerError.ToString());
                }
            }
        }
    }
}