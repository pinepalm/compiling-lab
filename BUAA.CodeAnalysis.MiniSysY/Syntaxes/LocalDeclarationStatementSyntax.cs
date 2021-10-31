using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class LocalDeclarationStatementSyntax : StatementSyntax
    {
        internal LocalDeclarationStatementSyntax()
            : base(SyntaxKind.LocalDeclarationStatement)
        {

        }

        public IReadOnlyList<SyntaxToken> Modifiers { get; init; }

        public VariableDeclarationSyntax Declaration { get; init; }

        public SyntaxToken SemicolonToken { get; init; }
    }
}
