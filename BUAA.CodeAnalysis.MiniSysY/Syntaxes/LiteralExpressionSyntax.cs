namespace BUAA.CodeAnalysis.MiniSysY
{
    public class LiteralExpressionSyntax : ExpressionSyntax
    {
        internal LiteralExpressionSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public SyntaxToken Token { get; init; }
    }
}
