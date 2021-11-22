using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class InitializerExpressionSyntax : ExpressionSyntax
    {
        internal InitializerExpressionSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public SyntaxToken OpenBraceToken { get; init; }

        public IReadOnlyList<ExpressionSyntax> Expressions { get; init; }

        public SyntaxToken CloseBraceToken { get; init; }
    }
}
