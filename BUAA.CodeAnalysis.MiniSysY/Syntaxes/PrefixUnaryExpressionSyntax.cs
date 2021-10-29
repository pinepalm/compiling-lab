namespace BUAA.CodeAnalysis.MiniSysY
{
    public class PrefixUnaryExpressionSyntax : ExpressionSyntax
    {
        internal PrefixUnaryExpressionSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public SyntaxToken OperatorToken { get; init; }

        public ExpressionSyntax Operand { get; init; }
    }
}
