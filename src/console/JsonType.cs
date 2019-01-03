using System;
using System.Collections.Generic;
using System.Linq;

namespace console
{
    class JsonType : IJsonType
    {
        public JsonType(IReadOnlyCollection<JsonProperty> properties)
        {
            Properties = properties;
        }
        
        public IReadOnlyCollection<JsonProperty> Properties { get; }

        public int ValueSize => Properties.Sum(v => v.ValueSize);

        public int ValueSizeOnDisk => Properties.Sum(v => v.ValueSizeOnDisk);

        public int SizeOnDisk => Properties.Sum(p => p.SizeOnDisk) + 2 + (Properties.Count == 0 ? 0 : Properties.Count - 1);

        public int SchemaSize => Properties.Sum(p => p.KeySize) + 2 + (Properties.Count == 0 ? 0 : Properties.Count - 1);

        public int MaxKeySize => Properties.Max(p => p.KeySize);

        public int MinKeySize => Properties.Min(p => p.KeySize);

        public int MaxValueSizeOnDisk => Properties.Max(p => p.ValueSizeOnDisk);

        public int MinValueSizeOnDisk => Properties.Min(p => p.ValueSizeOnDisk);

        public int MaxValueSize => Properties.Max(p => p.ValueSize);

        public int MinValueSize => Properties.Min(p => p.ValueSize);

        public JsonTypeEnum Type => JsonTypeEnum.Complex;

        public string Path { get; set; }

        public JsonTokenType TokenType => JsonTokenType.Complex;

        public static JsonType Empty(string path) => new JsonType(Array.Empty<JsonProperty>()) { Path = path };

        public void Accept(IJsonTokenVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
