namespace BUAA.CodeAnalysis.MiniSysY
{
    public abstract class ExpressionOrPatternSyntax : SyntaxNode
    {
        internal ExpressionOrPatternSyntax(SyntaxKind kind)
            : base(kind)
        {

        }
    }
}
