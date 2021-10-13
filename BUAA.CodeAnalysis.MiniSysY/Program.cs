using System;

namespace BUAA.CodeAnalysis.MiniSysY
{
    class Program
    {
        static void Main(string[] args)
        {
            var lexer = new MiniSysYLexicalAnalyzer(args[0]);
            var tokens = lexer.AnalyseAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            tokens?.ForEach((token) => Console.WriteLine(token));
        }
    }
}
