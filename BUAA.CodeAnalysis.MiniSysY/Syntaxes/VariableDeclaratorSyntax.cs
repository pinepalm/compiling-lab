namespace BUAA.CodeAnalysis.MiniSysY
{
    public class VariableDeclaratorSyntax : SyntaxNode
    {
        internal VariableDeclaratorSyntax()
            : base(SyntaxKind.VariableDeclarator)
        {

        }

        public SyntaxToken Identifier { get; init; }

        public EqualsValueClauseSyntax Initializer { get; init; }
    }
}
