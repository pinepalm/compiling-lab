using BUAA.CodeAnalysis.MiniSysY.Internals;
using System;
using System.IO;
using System.Linq;

namespace BUAA.CodeAnalysis.MiniSysY
{
    class Program
    {

#if DEBUG
        private const string TestMiniSysYCodeText =
@"int main() {
    const int ch = 48;
    int i = 1;
    while (i < 12) {
        int j = 0;
        while (1 == 1) {
            if (j % 3 == 1) {
                putch(ch + 1);
            } else {
                putch(ch);
            }
            j = j + 1;
            if (j >= 2 * i - 1)
                break;
        }
        putch(10);
        i = i + 1;
        continue; // something meaningless
    }
    return 0;
}";
#endif

        static void Main(string[] args)
        {
            try
            {

#if DEBUG
                var lexer = new Lexer(TestMiniSysYCodeText);
#else
                using var input = new StreamReader(args[0]);
                
                var text = input.ReadToEnd();
                var lexer = new Lexer(text);

                input.Close();
#endif

                var tokens = lexer.Analyse(out var trivias);

#if DEBUG
                Console.WriteLine("SyntaxTokens ->");
                Console.WriteLine("------------------------------");
                tokens?.ToList().ForEach((token) => Console.WriteLine($"{token.Text}: {token.Kind}"));
                Console.WriteLine("******************************");
                Console.WriteLine("SyntaxTrivias ->");
                Console.WriteLine("------------------------------");
                trivias?.ToList().ForEach((trivia) => Console.WriteLine($"{trivia.Kind}"));
                Console.WriteLine("******************************");
#endif

                var parser = new SyntaxParser(tokens);
                var tree = parser.Parse().WithRuntimeMethods().AsSyntaxTree();

                var builder = new LLVMIRBuilder(tree);
                var llvmIR = builder.Realize();

#if DEBUG
                Console.WriteLine("LLVM IR ->");
                Console.WriteLine("------------------------------");
                Console.WriteLine(llvmIR);
                Console.WriteLine("******************************");
#else
                using var output = new StreamWriter(args[1]);

                output.Write(llvmIR);
                output.Flush();
                output.Close();
#endif

            }
            catch (Exception)
            {
                Environment.Exit(1);
            }
        }
    }
}
