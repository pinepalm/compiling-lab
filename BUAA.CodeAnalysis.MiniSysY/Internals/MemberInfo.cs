using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY.Internals
{
    public class MemberInfo
    {
        internal MemberInfo()
        {

        }

        public string Name { get; init; }

        public string ActualName { get; init; }

        public string Kind { get; init; }

        public SyntaxNode Node { get; init; }

        public Dictionary<string, object> Properties { get; } = new();

        public MemberInfo WithProperty(string key, object value)
        {
            Properties.Add(key, value);

            return this;
        }
    }
}
