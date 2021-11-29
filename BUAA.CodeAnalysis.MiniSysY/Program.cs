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
@"int global_variable = 0;  // 全局变量

int add() {
    global_variable = global_variable + 1;
    return 1;
}

int main() {
    if (1 == 1 || 1 == add()) {} // 我们并不想 add() 函数被调用
    return global_variable;
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
#if DEBUG
            finally
            {

            }
#else
            catch (Exception)
            {
                Environment.Exit(1);
            }
#endif
        }
    }
}
