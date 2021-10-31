namespace BUAA.CodeAnalysis.MiniSysY
{
    public abstract class SimpleNameSyntax : NameSyntax
    {
        internal SimpleNameSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public abstract SyntaxToken Identifier { get; init; }
    }
}
