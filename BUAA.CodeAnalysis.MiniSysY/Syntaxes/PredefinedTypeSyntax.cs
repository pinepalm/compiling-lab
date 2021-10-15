namespace BUAA.CodeAnalysis.MiniSysY
{
    public class PredefinedTypeSyntax : TypeSyntax
    {
        internal PredefinedTypeSyntax()
            : base(SyntaxKind.PredefinedType)
        {

        }

        public SyntaxToken Keyword { get; init; }
    }
}
