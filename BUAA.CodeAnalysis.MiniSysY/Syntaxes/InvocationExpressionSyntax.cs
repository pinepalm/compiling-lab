namespace BUAA.CodeAnalysis.MiniSysY
{
    public class InvocationExpressionSyntax : ExpressionSyntax
    {
        internal InvocationExpressionSyntax()
            : base(SyntaxKind.InvocationExpression)
        {

        }

        public ExpressionSyntax Expression { get; init; }

        public ArgumentListSyntax ArgumentList { get; init; }
    }
}
