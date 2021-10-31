using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class EqualsValueClauseSyntax : SyntaxNode
    {
        internal EqualsValueClauseSyntax()
            : base(SyntaxKind.EqualsValueClause)
        {

        }

        public SyntaxToken EqualsToken { get; init; }

        public ExpressionSyntax Value { get; init; }
    }
}
