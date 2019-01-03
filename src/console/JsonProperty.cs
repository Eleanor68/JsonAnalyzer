namespace console
{
    class JsonProperty : IJsonToken
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public IJsonType Value { get; set; }
        public int KeySize => Name.Length + 2;
        public int ValueSizeOnDisk => Value.SizeOnDisk;
        public int ValueSize => Value.ValueSize;
        public int SizeOnDisk => ValueSizeOnDisk + KeySize + 1;

        public JsonTokenType TokenType => JsonTokenType.Property;

        public void Accept(IJsonTokenVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
