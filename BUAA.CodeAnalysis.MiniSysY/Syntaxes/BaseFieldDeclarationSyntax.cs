namespace BUAA.CodeAnalysis.MiniSysY
{
    public abstract class BaseFieldDeclarationSyntax : MemberDeclarationSyntax
    {
        internal BaseFieldDeclarationSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public abstract VariableDeclarationSyntax Declaration { get; init; }

        public abstract SyntaxToken SemicolonToken { get; init; }
    }
}
