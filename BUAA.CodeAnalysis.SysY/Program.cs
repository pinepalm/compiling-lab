using System;

namespace BUAA.CodeAnalysis.SysY
{
    class Program
    {
        static void Main(string[] args)
        {
            var lexer = new SysYLexicalAnalyzer(args[0]);
            var tokens = lexer.AnalyseAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            tokens?.ForEach((token) => Console.WriteLine(token));
        }
    }
}
