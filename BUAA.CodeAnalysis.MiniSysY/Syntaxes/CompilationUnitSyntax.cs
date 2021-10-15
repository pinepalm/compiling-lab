using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class CompilationUnitSyntax : SyntaxNode
    {
        internal CompilationUnitSyntax()
            : base(SyntaxKind.CompilationUnit)
        {

        }

        public IReadOnlyList<MemberDeclarationSyntax> Members { get; init; }

        public SyntaxTree AsSyntaxTree()
        {
            return new SyntaxTree() 
            {
                CompilationUnits = (new List<CompilationUnitSyntax>() 
                {
                    this
                }).AsReadOnly()
            };
        }
    }
}
