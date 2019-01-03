using System;
using System.Collections.Generic;
using System.Linq;

namespace console
{
    class JsonArray : IJsonType
    {
        public JsonArray(IReadOnlyCollection<IJsonType> values)
        {
            Values = values;
        }

        public IReadOnlyCollection<IJsonType> Values { get; }

        public int MaxKeySize => Values.Max(v => v.MaxKeySize);

        public int MaxValueSize => Values.Max(v => v.MaxValueSize);

        public int MaxValueSizeOnDisk => Values.Max(v => v.MaxValueSizeOnDisk);

        public int MinKeySize => Values.Min(v => v.MinKeySize);

        public int MinValueSize => Values.Min(v => v.MinValueSize);

        public int MinValueSizeOnDisk => Values.Min(v => v.MinValueSizeOnDisk);

        public int SchemaSize => Values.Sum(v => v.SchemaSize) + 2 + (Values.Count == 0 ? 0 : Values.Count - 1);

        public int SizeOnDisk => Values.Sum(v => v.SizeOnDisk) + 2 + (Values.Count == 0 ? 0 : Values.Count - 1);

        public int ValueSize => Values.Sum(v => v.ValueSize);

        public int ValueSizeOnDisk => Values.Sum(v => v.ValueSizeOnDisk);

        public JsonTypeEnum Type => JsonTypeEnum.Array;

        public string Path { get; set; }

        public JsonTokenType TokenType => JsonTokenType.Array;

        public static JsonArray Empty(string path) => new JsonArray(Array.Empty<IJsonType>()) { Path = path };

        public void Accept(IJsonTokenVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
