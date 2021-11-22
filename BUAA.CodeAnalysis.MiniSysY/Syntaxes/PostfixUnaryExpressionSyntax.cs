namespace BUAA.CodeAnalysis.MiniSysY
{
    public class PostfixUnaryExpressionSyntax : ExpressionSyntax
    {
        internal PostfixUnaryExpressionSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public ExpressionSyntax Operand { get; init; }

        public SyntaxToken OperatorToken { get; init; }
    }
}
