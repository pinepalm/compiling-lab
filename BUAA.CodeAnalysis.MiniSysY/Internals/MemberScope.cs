using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY.Internals
{
    public class MemberScope
    {
        internal MemberScope()
            : this(null)
        {

        }

        internal MemberScope(MemberScope parent)
        {
            Parent = parent;
        }

        public Dictionary<(string name, string type), (string regName, SyntaxNode node)> Members { get; } = new();

        public MemberScope Parent { get; }

        public bool TryLookup((string name, string type) key, out (string regName, SyntaxNode node) value)
        {
            var scope = this;

            while (scope is not null)
            {
                if (scope.Members.TryGetValue(key, out var tempValue))
                {
                    value = tempValue;

                    return true;
                }

                scope = scope.Parent;
            }

            value = (null, null);

            return false;
        }
    }
}
