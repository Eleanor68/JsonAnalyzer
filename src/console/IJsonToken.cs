namespace console
{
    interface IJsonToken
    {
        JsonTokenType TokenType { get; }
        void Accept(IJsonTokenVisitor visitor);
    }
}
