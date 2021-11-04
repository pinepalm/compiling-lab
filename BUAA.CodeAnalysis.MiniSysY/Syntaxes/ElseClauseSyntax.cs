namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ElseClauseSyntax : SyntaxNode
    {
        internal ElseClauseSyntax()
            : base(SyntaxKind.ElseClause)
        {

        }

        public SyntaxToken ElseKeyword { get; init; }

        public StatementSyntax Statement { get; init; }
    }
}
