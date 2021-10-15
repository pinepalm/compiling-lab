namespace BUAA.CodeAnalysis.MiniSysY
{
    public abstract class SyntaxNode
    {
        internal SyntaxNode(SyntaxKind kind)
        {
            Kind = kind;
        }

        public SyntaxKind Kind { get; }
    }
}
