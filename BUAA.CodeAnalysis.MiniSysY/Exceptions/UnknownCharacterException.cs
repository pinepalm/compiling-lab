using System;
using System.Runtime.Serialization;

namespace BUAA.CodeAnalysis.MiniSysY
{
    [Serializable]
    public class UnknownCharacterException : Exception
    {
        public UnknownCharacterException() { }
        public UnknownCharacterException(string message) : base(message) { }
        public UnknownCharacterException(string message, Exception inner) : base(message, inner) { }
        protected UnknownCharacterException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
