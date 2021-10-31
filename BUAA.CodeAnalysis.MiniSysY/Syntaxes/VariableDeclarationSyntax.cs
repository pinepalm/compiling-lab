using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class VariableDeclarationSyntax : SyntaxNode
    {
        internal VariableDeclarationSyntax()
            : base(SyntaxKind.VariableDeclaration)
        {

        }

        public TypeSyntax Type { get; init; }

        public IReadOnlyList<VariableDeclaratorSyntax> Variables { get; init; }
    }
}
