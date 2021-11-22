namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ArrayRankSpecifierSyntax : SyntaxNode
    {
        internal ArrayRankSpecifierSyntax()
            : base(SyntaxKind.ArrayRankSpecifier)
        {

        }

        public SyntaxToken OpenBracketToken { get; init; }

        public ExpressionSyntax Size { get; init; }

        public SyntaxToken CloseBracketToken { get; init; }
    }
}
