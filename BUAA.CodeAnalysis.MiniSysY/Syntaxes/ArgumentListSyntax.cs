using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class ArgumentListSyntax : BaseArgumentListSyntax
    {
        internal ArgumentListSyntax()
            : base(SyntaxKind.ArgumentList)
        {

        }

        public SyntaxToken OpenParenToken { get; init; }

        public override IReadOnlyList<ArgumentSyntax> Arguments { get; init; }

        public SyntaxToken CloseParenToken { get; init; }
    }
}
