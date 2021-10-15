namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ParameterListSyntax : SyntaxNode
    {
        internal ParameterListSyntax()
            : base(SyntaxKind.ParameterList)
        {

        }

        public SyntaxToken OpenParenToken { get; init; }

        public SyntaxToken CloseParenToken { get; init; }
    }
}
