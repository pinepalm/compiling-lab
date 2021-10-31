namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ExpressionStatementSyntax : StatementSyntax
    {
        internal ExpressionStatementSyntax()
            : base(SyntaxKind.ExpressionStatement)
        {

        }

        public ExpressionSyntax Expression { get; init; }

        public SyntaxToken SemicolonToken { get; init; }
    }
}
