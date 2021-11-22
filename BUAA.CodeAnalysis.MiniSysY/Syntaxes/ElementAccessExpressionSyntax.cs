using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ElementAccessExpressionSyntax : ExpressionSyntax
    {
        internal ElementAccessExpressionSyntax()
            : base(SyntaxKind.ElementAccessExpression)
        {

        }

        public ExpressionSyntax Expression { get; init; }

        public IReadOnlyList<BracketedArgumentSyntax> Arguments { get; init; }
    }
}
