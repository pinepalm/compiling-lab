namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ParenthesizedExpressionSyntax : ExpressionSyntax
    {
        internal ParenthesizedExpressionSyntax()
            : base(SyntaxKind.ParenthesizedExpression)
        {

        }

        public SyntaxToken OpenParenToken { get; init; }

        public ExpressionSyntax Expression { get; init; }

        public SyntaxToken CloseParenToken { get; init; }
    }
}
