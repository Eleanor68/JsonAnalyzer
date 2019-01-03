namespace console
{
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

        public JsonTokenType TokenType => JsonTokenType.WellKnown;

        public static JsonWellKnownType True(string path) => new JsonWellKnownType { ValueSize = 1, ValueSizeOnDisk = 4, Type = JsonTypeEnum.Boolean, Path = path };
        public static JsonWellKnownType False(string path) => new JsonWellKnownType { ValueSize = 1, ValueSizeOnDisk = 5, Type = JsonTypeEnum.Boolean, Path = path };
        public static JsonWellKnownType Null(string path) => new JsonWellKnownType { ValueSize = 0, ValueSizeOnDisk = 4, Type = JsonTypeEnum.Null, Path = path };

        public void Accept(IJsonTokenVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
