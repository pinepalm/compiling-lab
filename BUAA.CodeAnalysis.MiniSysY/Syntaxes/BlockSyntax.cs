using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class BlockSyntax : StatementSyntax
    {
        internal BlockSyntax()
            : base(SyntaxKind.Block)
        {

        }

        public SyntaxToken OpenBraceToken { get; init; }

        public IReadOnlyList<StatementSyntax> Statements { get; init; }

        public SyntaxToken CloseBraceToken { get; init; }
    }
}
