using System;

namespace console
{
    abstract class JsonTokenVisitorBase : IJsonTokenVisitor
    {
        public void Visit(JsonProperty property)
        {
            throw new NotImplementedException();
        }

        public void Visit(IJsonType type)
        {
            throw new NotImplementedException();
        }

        public void Visit(IJsonToken token)
        {
            throw new NotImplementedException();
        }
    }
}
