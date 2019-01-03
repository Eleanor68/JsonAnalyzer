namespace console
{
    interface IJsonType : IJsonToken
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
}
