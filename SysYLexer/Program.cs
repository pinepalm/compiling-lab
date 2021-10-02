using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SysYLexer
{
    class Program
    {
        private static Dictionary<string, TokenType> _keywords = new()
        {
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "while", TokenType.While },
            { "break", TokenType.Break },
            { "continue", TokenType.Continue },
            { "return", TokenType.Return }
        };

        private static Dictionary<string, TokenType> _delimiters = new()
        {
            { "=", TokenType.Assign },
            { ";", TokenType.Semicolon },
            { "(", TokenType.LPar },
            { ")", TokenType.RPar },
            { "{", TokenType.LBrace },
            { "}", TokenType.RBrace },
            { "+", TokenType.Plus },
            { "*", TokenType.Mult },
            { "/", TokenType.Div },
            { "<", TokenType.Lt },
            { ">", TokenType.Gt },
            { "==", TokenType.Eq }
        };

        private static List<Token> ReadTokens(StreamReader input)
        {
            var tokens = new List<Token>();
            var tokenBuilder = new StringBuilder();
            var tokenType = default(TokenType);
            var tokenText = default(string);
            var line = default(string);

            while ((line = input.ReadLine()) is not null)
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
                            tokens.Add(new Token() { Type = tokenType });
                        }
                        else
                        {
                            tokens.Add(new Token() { Type = TokenType.Ident, Text = tokenText });
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
                        tokens.Add(new Token() { Type = TokenType.Number, Text = tokenText });

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
                        tokens.Add(new Token() { Type = tokenType });

                        continue;
                    }

                    tokens.Add(new Token() { Type = TokenType.Err });
                    return tokens;
                }
            }

            return tokens;
        }

        private static void WriteTokens(List<Token> tokens)
        {
            tokens?.ForEach((token) => Console.WriteLine(token));
        }

        static void Main(string[] args)
        {
            using var input = new StreamReader(args[0]);

            var tokens = ReadTokens(input);
            WriteTokens(tokens);
        }
    }
}
