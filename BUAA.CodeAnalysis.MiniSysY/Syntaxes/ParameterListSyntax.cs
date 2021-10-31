using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ParameterListSyntax : BaseParameterListSyntax
    {
        internal ParameterListSyntax()
            : base(SyntaxKind.ParameterList)
        {

        }

        public SyntaxToken OpenParenToken { get; init; }

        public override IReadOnlyList<ParameterSyntax> Parameters { get; init; }

        public SyntaxToken CloseParenToken { get; init; }
    }
}
