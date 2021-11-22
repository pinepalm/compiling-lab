namespace BUAA.CodeAnalysis.MiniSysY
{
    public class BracketedArgumentSyntax : SyntaxNode
    {
        internal BracketedArgumentSyntax()
            : base(SyntaxKind.BracketedArgument)
        {

        }

        public SyntaxToken OpenBracketToken { get; init; }

        public ArgumentSyntax Argument { get; init; }

        public SyntaxToken CloseBracketToken { get; init; }
    }
}
