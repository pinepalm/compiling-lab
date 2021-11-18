namespace BUAA.CodeAnalysis.MiniSysY
{
    public class WhileStatementSyntax : StatementSyntax
    {
        internal WhileStatementSyntax()
            : base(SyntaxKind.WhileStatement)
        {

        }

        public SyntaxToken WhileKeyword { get; init; }

        public SyntaxToken OpenParenToken { get; init; }

        public ExpressionSyntax Condition { get; init; }

        public SyntaxToken CloseParenToken { get; init; }

        public StatementSyntax Statement { get; init; }
    }
}
