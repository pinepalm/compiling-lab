using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public abstract class BaseParameterListSyntax : SyntaxNode
    {
        internal BaseParameterListSyntax(SyntaxKind kind)
            : base(kind)
        {

        }

        public abstract IReadOnlyList<ParameterSyntax> Parameters { get; init; }
    }
}
