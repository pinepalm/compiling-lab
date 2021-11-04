namespace BUAA.CodeAnalysis.MiniSysY
{
    public class IfStatementSyntax : StatementSyntax
    {
        internal IfStatementSyntax()
            : base(SyntaxKind.IfStatement)
        {

        }

        public SyntaxToken IfKeyword { get; init; }

        public SyntaxToken OpenParenToken { get; init; }

        public ExpressionSyntax Condition { get; init; }

        public SyntaxToken CloseParenToken { get; init; }

        public StatementSyntax Statement { get; init; }

        public ElseClauseSyntax Else { get; init; }
    }
}
