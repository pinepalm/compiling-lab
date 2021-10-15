namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ExpressionSyntax : SyntaxNode
    {
        internal ExpressionSyntax()
            : base(SyntaxKind.Expression)
        {

        }

        public SyntaxToken NumericLiteralToken { get; init; }
    }
}
