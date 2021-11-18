namespace BUAA.CodeAnalysis.MiniSysY
{
    public class BreakStatementSyntax : StatementSyntax
    {
        internal BreakStatementSyntax()
            : base(SyntaxKind.BreakStatement)
        {

        }

        public SyntaxToken BreakKeyword { get; init; }

        public SyntaxToken SemicolonToken { get; init; }
    }
}
