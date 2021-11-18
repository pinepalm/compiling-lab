namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ContinueStatementSyntax : StatementSyntax
    {
        internal ContinueStatementSyntax()
            : base(SyntaxKind.ContinueStatement)
        {

        }

        public SyntaxToken ContinueKeyword { get; init; }

        public SyntaxToken SemicolonToken { get; init; }
    }
}
