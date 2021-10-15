namespace BUAA.CodeAnalysis.MiniSysY
{
    public class MethodDeclarationSyntax : MemberDeclarationSyntax
    {
        internal MethodDeclarationSyntax()
            : base(SyntaxKind.MethodDeclaration)
        {

        }

        public TypeSyntax ReturnType { get; init; }

        public SyntaxToken IdentifierToken { get; init; }

        public ParameterListSyntax ParameterList { get; init; }

        public BlockSyntax Body { get; init; }
    }
}
