using System;
using System.Runtime.Serialization;

namespace BUAA.CodeAnalysis.MiniSysY
{
    [Serializable]
    public class SyntaxException : Exception
    {
        public SyntaxException() { }
        public SyntaxException(string message) : base(message) { }
        public SyntaxException(string message, Exception inner) : base(message, inner) { }
        protected SyntaxException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
