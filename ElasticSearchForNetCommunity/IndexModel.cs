using System;
using Nest;

namespace ElasticSearchForNetCommunity
{
    [ElasticsearchType(Name = "IndexModel", IdProperty = nameof(Id))]
    public class IndexModel
    {
        [Keyword(Name = "id")]
        public Guid Id { get; set; }

        [Number(NumberType.Integer, Name = "number")]
        public int Number { get; set; }

        [Text]
        public string Text { get; set; }

        [GeoPoint(Name = "position")]
        public GeoCoordinate Position { get; set; }

        [Boolean(Name = "boolean")]
        public bool Boolean { get; set; }

        [Number(NumberType.ScaledFloat, Name = "scaledFloat", ScalingFactor = 100)]
        public float ScaledFloat { get; set; }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(Number)}: {Number}, {nameof(Text)}: {Text}, {nameof(Position)}: {Position}, {nameof(Boolean)}: {Boolean}, {nameof(ScaledFloat)}: {ScaledFloat}";
        }
    }
}