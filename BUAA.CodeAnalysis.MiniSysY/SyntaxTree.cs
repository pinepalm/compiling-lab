using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class SyntaxTree
    {
        public IReadOnlyList<CompilationUnitSyntax> CompilationUnits { get; init; }
    }
}
