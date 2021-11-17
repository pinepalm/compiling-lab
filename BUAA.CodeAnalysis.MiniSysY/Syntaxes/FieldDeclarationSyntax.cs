using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class FieldDeclarationSyntax : BaseFieldDeclarationSyntax
    {
        internal FieldDeclarationSyntax()
            : base(SyntaxKind.FieldDeclaration)
        {

        }

        public override IReadOnlyList<SyntaxToken> Modifiers { get; init; }

        public override VariableDeclarationSyntax Declaration { get; init; }

        public override SyntaxToken SemicolonToken { get; init; }
    }
}
