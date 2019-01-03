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
    class InputOptions
    {
        public bool TraceEnabled { get; set; }

        public long MinValueSize { get; set; }

        public long MaxValueSize { get; set; }

        public long Depth { get; set; }

        public string File { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            InputOptions options;

            try
            {
                options = ReadOptions(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            var sw = Stopwatch.StartNew();
            var jsonContent = File.ReadAllText(options.File, Encoding.UTF8);
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

            Console.WriteLine($"Json NON Formatted parsing elapsed time {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Json NON Formatted token tree size {ReadableBytes(totalMemory2 - totalMemory1)} bytes");
            Console.WriteLine($"Json NON Formatted content is {Percentage(bytes, totalMemory2 - totalMemory1)}% of the json token tree size");

            var s = ReadSchema(jt);

            Console.WriteLine();
            Console.WriteLine($"Json memory size {ReadableBytes(s.ValueSize)} this is {Percentage(s.ValueSize, totalMemory2 - totalMemory1)}% of json token tree size and {Percentage(s.ValueSize, notFormatedBytes)}% of json non formated content");

            if (options.TraceEnabled)
            {
                Console.WriteLine();
                Console.WriteLine($"Tracing json schema with max depth {options.Depth}");
                ShowSchemDetails(s, null, options);
            }

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

        static void ShowSchemDetails(IJsonType schema, IJsonType rootSchema, InputOptions options, int tabs = 0)
        {
            if (tabs >= options.Depth) return;
            var valueSize = schema.ValueSize;
            if (valueSize <= options.MinValueSize || valueSize >= options.MaxValueSize) return;
            var valueSizePercentage = Percentage(valueSize, rootSchema?.ValueSize ?? valueSize);

            Console.WriteLine($"{Tabs(tabs)}{schema.Path}-[schema:{ReadableBytes(schema.SchemaSize)}, value:{ReadableBytes(valueSize)}, value:{valueSizePercentage}%]");
            if (schema is JsonArray)
            {
                var array = schema as JsonArray;
                foreach (var v in array.Values)
                {
                    ShowSchemDetails(v, schema, options, tabs + 1);
                }
            }
            else if (schema is JsonType)
            {
                var type = schema as JsonType;
                foreach (var p in type.Properties)
                {
                    ShowSchemDetails(p.Value, schema, options, tabs + 1);
                }
            }
        }

        static string Tabs(int count) => new string(' ', count * 2);
        
        static decimal Percentage(long a, long b) => (a * 100) / b;

        static string ReadableBytes(long byteCount)
        {
            var sizes = new [] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0) return "0" + sizes[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + sizes[place];
        }

        public static InputOptions ReadOptions(string[] args)
        {
            var file = GetValue("--file=", null);

            if (string.IsNullOrEmpty(file))
            {
                throw new Exception("The file path is required, please provide it via `--file=` parameter.");
            }

            bool.TryParse(GetValue("--trace=", bool.TrueString), out bool traceEnabled);

            return new InputOptions
            {
                File = file,
                TraceEnabled = traceEnabled,
                Depth = long.Parse(GetValue("--depth=", "5")),
                MinValueSize = long.Parse(GetValue("--min-value-size=", "0")),
                MaxValueSize = long.Parse(GetValue("--max-value-size=", long.MaxValue.ToString())),
            };

            string GetValue(string key, string defaultValue)
            {
                foreach (var arg in args)
                {
                    if (arg.StartsWith(key))
                    {
                        return arg.Remove(0, key.Length);
                    }
                }

                return defaultValue;
            }
        }
    }
}
