namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ArgumentSyntax : SyntaxNode
    {
        internal ArgumentSyntax()
            : base(SyntaxKind.Argument)
        {

        }

        public ExpressionSyntax Expression { get; init; }
    }
}
