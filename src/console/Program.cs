using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace console
{
    class JsonProperty
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public IJsonType Value { get; set; }
        public int KeySize => Name.Length + 2;
        public int ValueSizeOnDisk => Value.SizeOnDisk;
        public int ValueSize => Value.ValueSize;
        public int SizeOnDisk => ValueSizeOnDisk + KeySize + 1;
    }

    interface IJsonType
    {
        JsonTypeEnum Type { get; }

        string Path { get; set; }

        int MaxKeySize { get; }
        int MaxValueSize { get; }
        int MaxValueSizeOnDisk { get; }
        int MinKeySize { get; }
        int MinValueSize { get; }
        int MinValueSizeOnDisk { get; }
        int SchemaSize { get; }
        int SizeOnDisk { get; }
        int ValueSize { get; }
        int ValueSizeOnDisk { get; }
    }

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

        public static JsonType Empty(string path) => new JsonType(Array.Empty<JsonProperty>()) { Path = path };
    }

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

        public static JsonArray Empty(string path) => new JsonArray(Array.Empty<IJsonType>()) { Path = path };
    }

    enum JsonTypeEnum
    {
        Null,
        Complex,
        Array,
        Integer,
        String,
        Decimal,
        Boolean
    }

    class JsonWellKnownType : IJsonType
    {
        public int MaxKeySize => 0;

        public int MaxValueSize => ValueSize;

        public int MaxValueSizeOnDisk => ValueSizeOnDisk;

        public int MinKeySize => 0;

        public int MinValueSize => ValueSize;

        public int MinValueSizeOnDisk => ValueSizeOnDisk;

        public int SchemaSize => 0;

        public int SizeOnDisk => ValueSizeOnDisk;

        public int ValueSize { get; set; }

        public int ValueSizeOnDisk { get; set; }

        public JsonTypeEnum Type { get; private set; }

        public string Path { get; set; }

        public static JsonWellKnownType True(string path) => new JsonWellKnownType { ValueSize = 1, ValueSizeOnDisk = 4, Type = JsonTypeEnum.Boolean, Path = path };
        public static JsonWellKnownType False(string path) => new JsonWellKnownType { ValueSize = 1, ValueSizeOnDisk = 5, Type = JsonTypeEnum.Boolean, Path = path };
        public static JsonWellKnownType Null(string path) => new JsonWellKnownType { ValueSize = 0, ValueSizeOnDisk = 4, Type = JsonTypeEnum.Null, Path = path };
    }

    class Program
    {
        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();
            var jsonContent = File.ReadAllText("manifest.json", Encoding.UTF8);
            sw.Stop();

            Console.WriteLine($"File read elapsed time {sw.ElapsedMilliseconds}ms");

            var bytes = Encoding.UTF8.GetByteCount(jsonContent);
            Console.WriteLine($"Json content size {ReadableBytes(bytes)}");

            var totalMemory1 = GC.GetTotalMemory(true);
            sw.Start();
            var jt = JToken.Parse(jsonContent);
            sw.Stop();
            var totalMemory2 = GC.GetTotalMemory(true);

            Console.WriteLine($"Json parsing elapsed time {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Json token tree size {ReadableBytes(totalMemory2 - totalMemory1)}");
            Console.WriteLine($"Json content is {Percentage(bytes, totalMemory2 - totalMemory1)}% of the json token tree size");

            var jsonContentNotFormated = jt.ToString(Formatting.None);
            var notFormatedBytes = Encoding.UTF8.GetByteCount(jsonContentNotFormated);

            Console.WriteLine($"Remove formatting and you will save {Percentage(notFormatedBytes, bytes)}% of size (not formated size {ReadableBytes(notFormatedBytes)})");
            totalMemory1 = GC.GetTotalMemory(true);
            sw.Start();
            jt = JToken.Parse(jsonContentNotFormated);
            sw.Stop();
            totalMemory2 = GC.GetTotalMemory(true);

            Console.WriteLine($"Json NON Formated parsing elapsed time {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Json NON Formated token tree size {ReadableBytes(totalMemory2 - totalMemory1)} bytes");
            Console.WriteLine($"Json NON Formated content is {Percentage(bytes, totalMemory2 - totalMemory1)}% of the json token tree size");

            var s = ReadSchema(jt);

            Console.WriteLine();
            Console.WriteLine($"Json memory size {ReadableBytes(s.ValueSize)} this is {Percentage(s.ValueSize, totalMemory2 - totalMemory1)}% of json token tree size and {Percentage(s.ValueSize, notFormatedBytes)}% of json non formated content");

            var maxDepth = 5;
            Console.WriteLine();
            Console.WriteLine($"Tracing json schema with max depth {maxDepth}");
            ShowSchemDetails(s, maxDepth);

            Console.ReadKey();
        }

        static IJsonType ReadSchema(JToken token)
        {
            if (token.Type == JTokenType.String || token.Type == JTokenType.Uri || token.Type == JTokenType.TimeSpan || token.Type == JTokenType.Guid)
            {
                var s = token.Value<string>();
                return new JsonWellKnownType { ValueSize = s.Length, ValueSizeOnDisk = s.Length + 2, Path = token.Path };
            }
            else if (token.Type == JTokenType.Integer)
            {
                var l = (long)(token as JValue).Value;
                return new JsonWellKnownType { ValueSize = Math.Abs(l) > int.MaxValue ? 8 : 4, ValueSizeOnDisk = GetSizeOnDisk(l), Path = token.Path };
            }
            else if (token.Type == JTokenType.Float)
            {
                var s = token.Value<string>();
                return new JsonWellKnownType { ValueSize = s.Length, ValueSizeOnDisk = s.Length + 2, Path = token.Path };
            }
            else if (token.Type == JTokenType.Boolean)
            {
                return (bool)(token as JValue).Value ? JsonWellKnownType.True(token.Path) : JsonWellKnownType.False(token.Path);
            }
            else if (token.Type == JTokenType.Null)
            {
                return JsonWellKnownType.Null(token.Path);
            }
            else if (token.Type == JTokenType.Array)
            {
                if (!token.HasValues) return JsonArray.Empty(token.Path);
                return new JsonArray(token.Children().Select(vt => ReadSchema(vt)).ToArray()) { Path = token.Path };
            }
            else if (token.Type == JTokenType.Object)
            {
                if (!token.HasValues) return JsonType.Empty(token.Path);
                var properties = new List<JsonProperty>();
                foreach (JProperty p in token.Children())
                {
                    var jp = new JsonProperty { Path = p.Path, Name = p.Name, Value = ReadSchema(p.Value), };
                    properties.Add(jp);
                }

                return new JsonType(properties) { Path = token.Path };
            }

            throw new Exception($"Not supported token type '{token.Type}'.");
        }

        public static byte GetSizeOnDisk(long v)
        {
            byte l = v < 0 ? (byte)1 : (byte)0;
            v = Math.Abs(v);

            for (; v > 0; l++, v /= 10) ;

            return l;
        }

        static void ShowSchemDetails(IJsonType schema, int maxDepth = 5, int tabs = 0)
        {
            if (tabs >= maxDepth) return;

            Console.WriteLine($"{Tabs(tabs)}{schema.Path}-[schema:{ReadableBytes(schema.SchemaSize)}, value:{ReadableBytes(schema.ValueSize)}]");
            if (schema is JsonArray)
            {
                var array = schema as JsonArray;
                foreach (var v in array.Values)
                {
                    ShowSchemDetails(v, maxDepth, tabs + 1);
                }
            }
            else if (schema is JsonType)
            {
                var type = schema as JsonType;
                foreach (var p in type.Properties)
                {
                    ShowSchemDetails(p.Value, maxDepth, tabs + 1);
                }
            }
        }

        static string Tabs(int count) => new string(' ', count * 2);
        
        static decimal Percentage(long a, long b) => (a * 100) / b;

        static String ReadableBytes(long byteCount)
        {
            var sizes = new [] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0) return "0" + sizes[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + sizes[place];
        }
    }
}
