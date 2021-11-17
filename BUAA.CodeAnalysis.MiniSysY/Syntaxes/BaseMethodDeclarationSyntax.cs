namespace BUAA.CodeAnalysis.MiniSysY
{
    public abstract class BaseMethodDeclarationSyntax : MemberDeclarationSyntax
    {
        internal BaseMethodDeclarationSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public abstract ParameterListSyntax ParameterList { get; init; }

        public abstract BlockSyntax Body { get; init; }

        public abstract SyntaxToken? SemicolonToken { get; init; }
    }
}
