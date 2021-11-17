using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class MethodDeclarationSyntax : BaseMethodDeclarationSyntax
    {
        internal MethodDeclarationSyntax()
            : base(SyntaxKind.MethodDeclaration)
        {

        }

        public override IReadOnlyList<SyntaxToken> Modifiers { get; init; }

        public TypeSyntax ReturnType { get; init; }

        public SyntaxToken Identifier { get; init; }

        public override ParameterListSyntax ParameterList { get; init; }

        public override BlockSyntax Body { get; init; }

        public override SyntaxToken? SemicolonToken { get; init; }
    }
}
