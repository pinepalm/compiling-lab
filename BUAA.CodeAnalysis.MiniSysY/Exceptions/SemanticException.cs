using System;
using System.Runtime.Serialization;

namespace BUAA.CodeAnalysis.MiniSysY
{
    [Serializable]
    public class SemanticException : Exception
    {
        public SemanticException() { }
        public SemanticException(string message) : base(message) { }
        public SemanticException(string message, Exception inner) : base(message, inner) { }
        protected SemanticException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
