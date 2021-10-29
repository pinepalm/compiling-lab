namespace BUAA.CodeAnalysis.MiniSysY
{
    public class BinaryExpressionSyntax : ExpressionSyntax
    {
        internal BinaryExpressionSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public ExpressionSyntax Left { get; init; }
        
        public SyntaxToken OperatorToken { get; init; }

        public ExpressionSyntax Right { get; init; }
    }
}
