using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public abstract class BaseParameterSyntax : SyntaxNode
    {
        internal BaseParameterSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public abstract IReadOnlyList<SyntaxToken> Modifiers { get; init; }

        public abstract TypeSyntax Type { get; init; }
    }
}
