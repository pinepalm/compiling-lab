using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public abstract class BaseArgumentListSyntax : SyntaxNode
    {
        internal BaseArgumentListSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public abstract IReadOnlyList<ArgumentSyntax> Arguments { get; init; }
    }
}
