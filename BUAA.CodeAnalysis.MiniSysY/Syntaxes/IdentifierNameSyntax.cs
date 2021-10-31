namespace BUAA.CodeAnalysis.MiniSysY
{
    public class IdentifierNameSyntax : SimpleNameSyntax
    {
        internal IdentifierNameSyntax()
            : base(SyntaxKind.IdentifierName)
        {

        }

        public override SyntaxToken Identifier { get; init; }
    }
}
