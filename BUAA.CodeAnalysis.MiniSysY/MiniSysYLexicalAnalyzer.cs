using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public partial class MiniSysYLexicalAnalyzer
    {
        private readonly string _inputFile;

        public MiniSysYLexicalAnalyzer(string inputFile)
        {
            _inputFile = inputFile;
        }

        public async Task<List<MiniSysYToken>> AnalyseAsync()
        {
            using var input = new StreamReader(_inputFile);

            var tokens = new List<MiniSysYToken>();
            var tokenBuilder = new StringBuilder();
            var tokenType = default(MiniSysYTokenType);
            var tokenText = default(string);
            var line = default(string);

            while ((line = await input.ReadLineAsync()) is not null)
            {
                int charIndex = 0;

                line = line.Trim();
                while (charIndex < line.Length)
                {
                    char currentChar = line[charIndex];

                    while (currentChar.IsWhiteSpace())
                    {
                        currentChar = line[++charIndex];
                    }

                    if (currentChar.IsLetterOrUnderline())
                    {
                        tokenBuilder.Clear();
                        while (currentChar.IsLetterOrUnderlineOrDigit())
                        {
                            tokenBuilder.Append(currentChar);

                            charIndex++;
                            if (charIndex < line.Length)
                            {
                                currentChar = line[charIndex];
                            }
                            else
                            {
                                break;
                            }
                        }

                        tokenText = tokenBuilder.ToString();
                        if (_keywords.TryGetValue(tokenText, out tokenType))
                        {
                            tokens.Add(new MiniSysYToken() { Type = tokenType });
                        }
                        else
                        {
                            tokens.Add(new MiniSysYToken() { Type = MiniSysYTokenType.Ident, Text = tokenText });
                        }

                        continue;
                    }

                    if (currentChar.IsDigit())
                    {
                        tokenBuilder.Clear();
                        while (currentChar.IsDigit())
                        {
                            tokenBuilder.Append(currentChar);

                            charIndex++;
                            if (charIndex < line.Length)
                            {
                                currentChar = line[charIndex];
                            }
                            else
                            {
                                break;
                            }
                        }

                        tokenText = tokenBuilder.ToString();
                        tokens.Add(new MiniSysYToken() { Type = MiniSysYTokenType.Number, Text = tokenText });

                        continue;
                    }

                    tokenBuilder.Clear();
                    tokenBuilder.Append(currentChar);
                    charIndex++;
                    if (currentChar.IsEqualSign())
                    {
                        if (charIndex < line.Length)
                        {
                            currentChar = line[charIndex];
                            if (currentChar.IsEqualSign())
                            {
                                tokenBuilder.Append(currentChar);
                                charIndex++;
                            }
                        }
                    }

                    tokenText = tokenBuilder.ToString();
                    if (_delimiters.TryGetValue(tokenText, out tokenType))
                    {
                        tokens.Add(new MiniSysYToken() { Type = tokenType });

                        continue;
                    }

                    tokens.Add(new MiniSysYToken() { Type = MiniSysYTokenType.Err });
                    return tokens;
                }
            }

            return tokens;
        }
    }

    public partial class MiniSysYLexicalAnalyzer
    {
        private static Dictionary<string, MiniSysYTokenType> _keywords = new()
        {
            { "if", MiniSysYTokenType.If },
            { "else", MiniSysYTokenType.Else },
            { "while", MiniSysYTokenType.While },
            { "break", MiniSysYTokenType.Break },
            { "continue", MiniSysYTokenType.Continue },
            { "return", MiniSysYTokenType.Return }
        };

        private static Dictionary<string, MiniSysYTokenType> _delimiters = new()
        {
            { "=", MiniSysYTokenType.Assign },
            { ";", MiniSysYTokenType.Semicolon },
            { "(", MiniSysYTokenType.LPar },
            { ")", MiniSysYTokenType.RPar },
            { "{", MiniSysYTokenType.LBrace },
            { "}", MiniSysYTokenType.RBrace },
            { "+", MiniSysYTokenType.Plus },
            { "*", MiniSysYTokenType.Mult },
            { "/", MiniSysYTokenType.Div },
            { "<", MiniSysYTokenType.Lt },
            { ">", MiniSysYTokenType.Gt },
            { "==", MiniSysYTokenType.Eq }
        };
    }
}
