using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ParameterSyntax : BaseParameterSyntax
    {
        internal ParameterSyntax()
            : base(SyntaxKind.Parameter)
        {

        }

        public override IReadOnlyList<SyntaxToken> Modifiers { get; init; }

        public override TypeSyntax Type { get; init; }

        public SyntaxToken? Identifier { get; init; }
    }
}
