using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public abstract class MemberDeclarationSyntax : SyntaxNode
    {
        internal MemberDeclarationSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public abstract IReadOnlyList<SyntaxToken> Modifiers { get; init; }
    }
}
