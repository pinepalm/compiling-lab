using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BUAA.CodeAnalysis.SysY
{
    public partial class SysYLexicalAnalyzer
    {
        private readonly string _inputFile;

        public SysYLexicalAnalyzer(string inputFile)
        {
            _inputFile = inputFile;
        }

        public async Task<List<SysYToken>> AnalyseAsync()
        {
            using var input = new StreamReader(_inputFile);

            var tokens = new List<SysYToken>();
            var tokenBuilder = new StringBuilder();
            var tokenType = default(SysYTokenType);
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
                            tokens.Add(new SysYToken() { Type = tokenType });
                        }
                        else
                        {
                            tokens.Add(new SysYToken() { Type = SysYTokenType.Ident, Text = tokenText });
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
                        tokens.Add(new SysYToken() { Type = SysYTokenType.Number, Text = tokenText });

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
                        tokens.Add(new SysYToken() { Type = tokenType });

                        continue;
                    }

                    tokens.Add(new SysYToken() { Type = SysYTokenType.Err });
                    return tokens;
                }
            }

            return tokens;
        }
    }

    public partial class SysYLexicalAnalyzer
    {
        private static Dictionary<string, SysYTokenType> _keywords = new()
        {
            { "if", SysYTokenType.If },
            { "else", SysYTokenType.Else },
            { "while", SysYTokenType.While },
            { "break", SysYTokenType.Break },
            { "continue", SysYTokenType.Continue },
            { "return", SysYTokenType.Return }
        };

        private static Dictionary<string, SysYTokenType> _delimiters = new()
        {
            { "=", SysYTokenType.Assign },
            { ";", SysYTokenType.Semicolon },
            { "(", SysYTokenType.LPar },
            { ")", SysYTokenType.RPar },
            { "{", SysYTokenType.LBrace },
            { "}", SysYTokenType.RBrace },
            { "+", SysYTokenType.Plus },
            { "*", SysYTokenType.Mult },
            { "/", SysYTokenType.Div },
            { "<", SysYTokenType.Lt },
            { ">", SysYTokenType.Gt },
            { "==", SysYTokenType.Eq }
        };
    }
}
