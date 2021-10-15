namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ReturnStatementSyntax : StatementSyntax
    {
        internal ReturnStatementSyntax()
            : base(SyntaxKind.ReturnStatement)
        {

        }

        public SyntaxToken ReturnKeyword { get; init; }

        public ExpressionSyntax Expression { get; init; }

        public SyntaxToken SemicolonToken { get; init; }
    }
}
