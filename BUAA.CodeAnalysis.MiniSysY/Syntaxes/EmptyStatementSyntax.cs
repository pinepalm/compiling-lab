namespace BUAA.CodeAnalysis.MiniSysY
{
    public class EmptyStatementSyntax : StatementSyntax
    {
        internal EmptyStatementSyntax()
            : base(SyntaxKind.EmptyStatement)
        {

        }

        public SyntaxToken SemicolonToken { get; init; }
    }
}
