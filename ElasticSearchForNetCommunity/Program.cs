﻿using System;
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
            var indexDescriptor =  new CreateIndexDescriptor(indexName)
                .Mappings(ms => ms
                    .Map<IndexModel>(m => m.AutoMap()))
                .Settings(s =>
                {
                    s = s.Setting("index.max_result_window", 50_000);
                    return s.NumberOfShards(4);
                });
            
            if (!(await client.IndexExistsAsync(indexName)).Exists)
            {
                var createIndexResponse = await client.CreateIndexAsync(indexDescriptor);
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
                        Position = new GeoCoordinate(random.NextDouble(), random.NextDouble()),
                        ScaledFloat = (float) random.NextDouble()
                    };
                    models.Add(newModel);
                }

                var response = await client.IndexManyAsync(models, indexName);
                HandleErrors(response);
            }

            var size = 100;
            var tasks = new List<Task>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                var sliceId = i;
                tasks.Add(Task.Run(async () =>
                {
                    var searchResult =
                        await client.SearchAsync<IndexModel>(sr => sr.Index(indexName).Size(size).Slice(s => s.Id(sliceId).Max(Environment.ProcessorCount)).Scroll("1m").Query(
                            qc =>
                                qc.Bool(bc => bc.Must(
                                    q => q.Term(t => t.Field(e => e.Boolean).Value(true)),
                                    q => q.Fuzzy(f => f.Field(e => e.Text).Value("Telt").Fuzziness(Fuzziness.Ratio(85)))
                                ))));
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
                }));
            }

            await Task.WhenAll(tasks);
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
