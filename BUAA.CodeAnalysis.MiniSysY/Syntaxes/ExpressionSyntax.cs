namespace BUAA.CodeAnalysis.MiniSysY
{
    public abstract class ExpressionSyntax : ExpressionOrPatternSyntax
    {
        internal ExpressionSyntax(SyntaxKind kind)
            : base(kind)
        {

        }
    }
}
