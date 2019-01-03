namespace console
{
    interface IJsonTokenVisitor
    {
        void Visit(JsonProperty property);
        void Visit(IJsonType type);
        void Visit(IJsonToken token);
    }
}
