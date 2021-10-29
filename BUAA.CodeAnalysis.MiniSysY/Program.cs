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
@"int main() { // mian
    return /* 123 */ 3 + 6 - 4 * 8 * 1 + 5 * 2;
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
                var tree = parser.Parse().AsSyntaxTree();

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
