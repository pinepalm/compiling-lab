using System;
using System.Collections.Generic;
using System.Text;

namespace BUAA.CodeAnalysis.MiniSysY.Internals
{
    internal partial class Lexer
    {
        private readonly string _text;

        public Lexer(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        private IReadOnlyList<SyntaxToken> Analyse(bool includeTrivia, out IReadOnlyList<SyntaxTrivia> syntaxTrivias)
        {
            var textViewer = new TextViewer(_text);

            var tokens = new List<SyntaxToken>();
            var trivias = includeTrivia ? new List<SyntaxTrivia>() : null;

            while (!textViewer.IsIndexAtEnd)
            {
                char c = textViewer.PeekChar();

                switch (c)
                {
                    case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_':
                        ScanIdentifierOrKeyword();

                        break;
                    case >= '0' and <= '9':
                        ScanNumericLiteral();

                        break;
                    case '(':
                    case ')':
                    case '{':
                    case '}':
                    case ';':
                    case ',':
                        {
                            var delimiter = c.ToString();

                            if (_delimiters.TryGetValue(delimiter, out var tokenKind))
                            {
                                textViewer.AdvanceChar(delimiter.Length);
                                tokens.Add(new SyntaxToken() { Kind = tokenKind, Text = delimiter });
                            }
                            else
                            {
                                goto default;
                            }
                        }

                        break;
                    case '/':
                        {
                            char peekChar = textViewer.PeekChar(1);

                            if (peekChar is '/')
                            {
                                ScanSingleLineComment();
                            }
                            else if (peekChar is '*')
                            {
                                ScanMultiLineComment();
                            }
                            else
                            {
                                goto case '%';
                            }
                        }

                        break;
                    case '=':
                    case '+':
                    case '-':
                    case '*':
                    case '%':
                        {
                            var @operator = c.ToString();

                            if (_operators.TryGetValue(@operator, out var tokenKind))
                            {
                                textViewer.AdvanceChar(@operator.Length);
                                tokens.Add(new SyntaxToken() { Kind = tokenKind, Text = @operator });
                            }
                            else
                            {
                                goto default;
                            }
                        }

                        break;
                    default:
                        if (char.IsWhiteSpace(c))
                        {
                            textViewer.AdvanceChar();
                        }
                        else
                        {
                            throw new UnknownCharacterException();
                        }

                        break;
                }
            }

            syntaxTrivias = trivias?.AsReadOnly();
            return tokens.AsReadOnly();

            void ScanIdentifierOrKeyword()
            {
                var c = default(char);

                if ((c = textViewer.PeekChar()) is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_')
                {
                    var tokenBuilder = new StringBuilder();

                    tokenBuilder.Append(c);
                    textViewer.AdvanceChar();

                    while ((c = textViewer.PeekChar()) is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9'))
                    {
                        tokenBuilder.Append(c);
                        textViewer.AdvanceChar();
                    }

                    var tokenText = tokenBuilder.ToString();

                    tokens.Add(new SyntaxToken()
                    {
                        Kind = _keywords.TryGetValue(tokenText, out var tokenKind) ? tokenKind : SyntaxKind.IdentifierToken,
                        Text = tokenText,
                        Value = tokenText
                    });
                }
            }

            void ScanNumericLiteral()
            {
                var c = default(char);

                if ((c = textViewer.PeekChar()) is (>= '0' and <= '9'))
                {
                    var tokenBuilder = new StringBuilder();
                    int fromBase = 10;

                    tokenBuilder.Append(c);
                    textViewer.AdvanceChar();

                    if (c is '0')
                    {
                        c = textViewer.PeekChar();
                        switch (c)
                        {
                            case >= '0' and <= '7':
                                fromBase = 8;
                                tokenBuilder.Append(c);
                                textViewer.AdvanceChar();

                                while ((c = textViewer.PeekChar()) is (>= '0' and <= '7'))
                                {
                                    tokenBuilder.Append(c);
                                    textViewer.AdvanceChar();
                                }

                                break;
                            case 'x' or 'X':
                                {
                                    char peekChar = textViewer.PeekChar(1);

                                    if (peekChar is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F'))
                                    {
                                        fromBase = 16;
                                        tokenBuilder.Append(c);
                                        textViewer.AdvanceChar();

                                        while ((c = textViewer.PeekChar()) is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F'))
                                        {
                                            tokenBuilder.Append(c);
                                            textViewer.AdvanceChar();
                                        }
                                    }
                                }

                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        while ((c = textViewer.PeekChar()) is (>= '0' and <= '9'))
                        {
                            tokenBuilder.Append(c);
                            textViewer.AdvanceChar();
                        }
                    }

                    var tokenText = tokenBuilder.ToString();

                    tokens.Add(new SyntaxToken()
                    {
                        Kind = SyntaxKind.NumericLiteralToken,
                        Text = tokenText,
                        Value = Convert.ToInt32(fromBase is 16 ? tokenText.Substring(2) : tokenText, fromBase)
                    });
                }
            }

            void ScanSingleLineComment()
            {
                var c = default(char);

                if ((c = textViewer.PeekChar()) is '/')
                {
                    char peekChar = textViewer.PeekChar(1);

                    if (peekChar is '/')
                    {
                        textViewer.AdvanceChar(2);

                        while ((c = textViewer.PeekChar()) is not ('\r' or '\n' or TextViewer.InvalidCharacter))
                        {
                            textViewer.AdvanceChar();
                        }

                        trivias?.Add(new SyntaxTrivia()
                        {
                            Kind = SyntaxKind.SingleLineCommentTrivia
                        });
                    }
                }
            }

            void ScanMultiLineComment()
            {
                var c = default(char);

                if ((c = textViewer.PeekChar()) is '/')
                {
                    char peekChar = textViewer.PeekChar(1);

                    if (peekChar is '*')
                    {
                        textViewer.AdvanceChar(2);

                        while (((c = textViewer.PeekChar()) is not '*' || (peekChar = textViewer.PeekChar(1)) is not '/') &&
                            (c is not TextViewer.InvalidCharacter))
                        {
                            textViewer.AdvanceChar();
                        }

                        if (c is '*')
                        {
                            textViewer.AdvanceChar(2);
                        }
                        else
                        {
                            throw new SyntaxException();
                        }

                        trivias?.Add(new SyntaxTrivia()
                        {
                            Kind = SyntaxKind.MultiLineCommentTrivia
                        });
                    }
                }
            }
        }

        public IReadOnlyList<SyntaxToken> Analyse(out IReadOnlyList<SyntaxTrivia> syntaxTrivias)
        {
            return Analyse(includeTrivia: true, out syntaxTrivias);
        }

        public IReadOnlyList<SyntaxToken> Analyse()
        {
            return Analyse(includeTrivia: false, out _);
        }
    }

    internal partial class Lexer
    {
        private static readonly Dictionary<string, SyntaxKind> _keywords = new()
        {
            { "int", SyntaxKind.IntKeyword },
            { "return", SyntaxKind.ReturnKeyword },
            { "const", SyntaxKind.ConstKeyword }
        };

        private static readonly Dictionary<string, SyntaxKind> _delimiters = new()
        {
            { "(", SyntaxKind.OpenParenToken },
            { ")", SyntaxKind.CloseParenToken },
            { "{", SyntaxKind.OpenBraceToken },
            { "}", SyntaxKind.CloseBraceToken },
            { ";", SyntaxKind.SemicolonToken },
            { ",", SyntaxKind.CommaToken }
        };

        private static readonly Dictionary<string, SyntaxKind> _operators = new()
        {
            { "+", SyntaxKind.PlusToken },
            { "-", SyntaxKind.MinusToken },
            { "*", SyntaxKind.AsteriskToken },
            { "/", SyntaxKind.SlashToken },
            { "%", SyntaxKind.PercentToken },
            { "=", SyntaxKind.EqualsToken }
        };
    }
}
