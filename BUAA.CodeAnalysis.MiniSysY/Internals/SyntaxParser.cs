using System;
using System.Collections.Generic;
using System.Linq;

namespace BUAA.CodeAnalysis.MiniSysY.Internals
{
    internal partial class SyntaxParser
    {
        private readonly IReadOnlyList<SyntaxToken> _syntaxTokens;

        public SyntaxParser(IReadOnlyList<SyntaxToken> syntaxTokens)
        {
            _syntaxTokens = syntaxTokens ?? throw new ArgumentNullException(nameof(syntaxTokens));
        }

        public CompilationUnitSyntax Parse()
        {
            var tokenListViewer = new SyntaxTokenListViewer(_syntaxTokens);

            var members = new List<MemberDeclarationSyntax>();

            while (!tokenListViewer.IsIndexAtEnd)
            {
                SyntaxToken token = tokenListViewer.PeekToken();

                switch (token.Kind)
                {
                    case SyntaxKind.IntKeyword:
                        if (!ParseMethod())
                        {
                            goto default;
                        }

                        break;
                    default:
                        throw new SyntaxException();
                }
            }

            return new CompilationUnitSyntax() { Members = members.AsReadOnly() };

            bool ParseMethod()
            {
                // only 1 main method
                if (members.Count is 1)
                {
                    return false;
                }

                int position = tokenListViewer.Position;

                if (ParseReturnType(out var returnType) &&
                    ParseIdentifier(out var identifier) &&
                    ParseParameterList(out var parameterList) &&
                    ParseBody(out var body))
                {
                    members.Add(new MethodDeclarationSyntax()
                    {
                        ReturnType = returnType,
                        IdentifierToken = identifier,
                        ParameterList = parameterList,
                        Body = body
                    });

                    return true;
                }

                tokenListViewer.Reset(position);

                return false;
            }

            bool ParseReturnType(out TypeSyntax returnType)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.IntKeyword)
                {
                    tokenListViewer.AdvanceToken();

                    returnType = new PredefinedTypeSyntax() { Keyword = token };

                    return true;
                }

                returnType = null;

                return false;
            }

            bool ParseIdentifier(out SyntaxToken identifier)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.IdentifierToken)
                {
                    // only main identifier is valid
                    if (token.Value is "main")
                    {
                        tokenListViewer.AdvanceToken();

                        identifier = token;

                        return true;
                    }
                }

                identifier = SyntaxToken.Empty;

                return false;
            }

            bool ParseParameterList(out ParameterListSyntax parameterList)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.OpenParenToken)
                {
                    var peekToken = tokenListViewer.PeekToken(1);

                    if (peekToken.Kind is SyntaxKind.CloseParenToken)
                    {
                        tokenListViewer.AdvanceToken(2);

                        parameterList = new ParameterListSyntax() { OpenParenToken = token, CloseParenToken = peekToken };

                        return true;
                    }
                }

                parameterList = null;

                return false;
            }

            bool ParseBody(out BlockSyntax body)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.OpenBraceToken)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParseStatements(out var statements))
                    {
                        var endToken = tokenListViewer.PeekToken();

                        if (endToken.Kind is SyntaxKind.CloseBraceToken)
                        {
                            tokenListViewer.AdvanceToken();

                            body = new BlockSyntax()
                            {
                                OpenBraceToken = token,
                                Statements = statements,
                                CloseBraceToken = endToken
                            };

                            return true;
                        }
                    }
                }

                body = null;

                return false;
            }

            bool ParseStatements(out IReadOnlyList<StatementSyntax> statements)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.ReturnKeyword)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParseExpression(out var expression))
                    {
                        var endToken = tokenListViewer.PeekToken();

                        if (endToken.Kind is SyntaxKind.SemicolonToken)
                        {
                            tokenListViewer.AdvanceToken();

                            statements = (new List<StatementSyntax>()
                                    {
                                        new ReturnStatementSyntax()
                                        {
                                            ReturnKeyword = token,
                                            Expression = expression,
                                            SemicolonToken = endToken
                                        }
                                    }).AsReadOnly();

                            return true;
                        }
                    }
                }

                statements = null;

                return false;
            }

            bool ParseExpression(out ExpressionSyntax expression)
            {
                // if (ParseExpressionCore(out var leftExpression, null))
                // {
                //     var token = tokenListViewer.PeekToken();

                //     if (_precedenceOfOperators.ContainsKey(token.Kind))
                //     {
                //         int position = tokenListViewer.Position;

                //         tokenListViewer.AdvanceToken();

                //         if (ParseExpression(out var rightExpression))
                //         {
                //             expression = new BinaryExpressionSyntax(_binaryExpressionKindOfOperators[token.Kind])
                //             {
                //                 Left = leftExpression,
                //                 OperatorToken = token,
                //                 Right = rightExpression
                //             };

                //             return true;
                //         }
                //         else
                //         {
                //             tokenListViewer.Reset(position);

                //             expression = leftExpression;

                //             return true;
                //         }
                //     }
                //     else
                //     {
                //         expression = leftExpression;

                //         return true;
                //     }
                // }

                // expression = null;

                // return false;

                var samePrecedenceExpressionLinkedList = new LinkedList<ExpressionSyntax>();
                var operatorLinkedList = new LinkedList<SyntaxToken>();
                int position = tokenListViewer.Position;

                while (ParseExpressionCore(out var samePrecedenceExpression))
                {
                    samePrecedenceExpressionLinkedList.AddLast(samePrecedenceExpression);

                    var token = tokenListViewer.PeekToken();

                    if (_precedenceOfOperators.ContainsKey(token.Kind))
                    {
                        position = tokenListViewer.Position;

                        tokenListViewer.AdvanceToken();

                        operatorLinkedList.AddLast(token);
                    }
                    else
                    {
                        break;
                    }
                }

                if (samePrecedenceExpressionLinkedList.Any())
                {
                    if (samePrecedenceExpressionLinkedList.Count == operatorLinkedList.Count)
                    {
                        operatorLinkedList.RemoveLast();
                        tokenListViewer.Reset(position);
                    }

                    while (operatorLinkedList.Any())
                    {
                        var leftExpression = samePrecedenceExpressionLinkedList.First();
                        samePrecedenceExpressionLinkedList.RemoveFirst();

                        var @operator = operatorLinkedList.First();
                        operatorLinkedList.RemoveFirst();

                        var rightExpression = samePrecedenceExpressionLinkedList.First();
                        samePrecedenceExpressionLinkedList.RemoveFirst();

                        var binaryExpression = new BinaryExpressionSyntax(_binaryExpressionKindOfOperators[@operator.Kind])
                        {
                            Left = leftExpression,
                            OperatorToken = @operator,
                            Right = rightExpression
                        };

                        samePrecedenceExpressionLinkedList.AddFirst(binaryExpression);
                    }

                    expression = samePrecedenceExpressionLinkedList.First();

                    return true;
                }

                expression = null;

                return false;
            }

            bool ParseExpressionCore(out ExpressionSyntax expression)
            {
                // if (ParsePrefixUnaryOrLiteralOrParenthesizedExpression(out var leftExpression))
                // {
                //     var token = tokenListViewer.PeekToken();

                //     if (_precedenceOfOperators.TryGetValue(token.Kind, out var precedenceOfOperator))
                //     {
                //         if (precedence is null || precedence == precedenceOfOperator)
                //         {
                //             int position = tokenListViewer.Position;

                //             tokenListViewer.AdvanceToken();

                //             if (ParseExpressionCore(out var rightExpression, precedenceOfOperator))
                //             {
                //                 expression = new BinaryExpressionSyntax(_binaryExpressionKindOfOperators[token.Kind])
                //                 {
                //                     Left = leftExpression,
                //                     OperatorToken = token,
                //                     Right = rightExpression
                //                 };

                //                 return true;
                //             }
                //             else
                //             {
                //                 tokenListViewer.Reset(position);

                //                 expression = leftExpression;

                //                 return true;
                //             }
                //         }
                //         else if (precedence > precedenceOfOperator)
                //         {
                //             expression = leftExpression;

                //             return true;
                //         }
                //         else
                //         {
                //             expression = null;

                //             return false;
                //         }
                //     }
                //     else
                //     {
                //         expression = leftExpression;

                //         return true;
                //     }
                // }

                // expression = null;

                // return false;

                var unaryExpressionLinkedList = new LinkedList<ExpressionSyntax>();
                var operatorLinkedList = new LinkedList<SyntaxToken>();
                int position = tokenListViewer.Position;
                int? precedence = null;

                while (ParsePrefixUnaryOrLiteralOrParenthesizedExpression(out var unaryExpression))
                {
                    unaryExpressionLinkedList.AddLast(unaryExpression);

                    var token = tokenListViewer.PeekToken();

                    if (_precedenceOfOperators.TryGetValue(token.Kind, out var precedenceOfOperator))
                    {
                        if (precedence is null || precedence == precedenceOfOperator)
                        {
                            position = tokenListViewer.Position;

                            tokenListViewer.AdvanceToken();

                            operatorLinkedList.AddLast(token);

                            precedence = precedenceOfOperator;
                        }
                        else if (precedence > precedenceOfOperator)
                        {
                            break;
                        }
                        else
                        {
                            unaryExpressionLinkedList.RemoveLast();
                            operatorLinkedList.RemoveLast();
                            tokenListViewer.Reset(position);

                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (unaryExpressionLinkedList.Any())
                {
                    while (operatorLinkedList.Any())
                    {
                        var leftExpression = unaryExpressionLinkedList.First();
                        unaryExpressionLinkedList.RemoveFirst();

                        var @operator = operatorLinkedList.First();
                        operatorLinkedList.RemoveFirst();

                        var rightExpression = unaryExpressionLinkedList.First();
                        unaryExpressionLinkedList.RemoveFirst();

                        var binaryExpression = new BinaryExpressionSyntax(_binaryExpressionKindOfOperators[@operator.Kind])
                        {
                            Left = leftExpression,
                            OperatorToken = @operator,
                            Right = rightExpression
                        };

                        unaryExpressionLinkedList.AddFirst(binaryExpression);
                    }

                    expression = unaryExpressionLinkedList.First();

                    return true;
                }

                expression = null;

                return false;
            }

            bool ParsePrefixUnaryOrLiteralOrParenthesizedExpression(out ExpressionSyntax expression)
            {
                var token = tokenListViewer.PeekToken();

                switch (token.Kind)
                {
                    case SyntaxKind.PlusToken:
                    case SyntaxKind.MinusToken:
                        return ParsePrefixUnaryExpression(out expression);

                    case SyntaxKind.NumericLiteralToken:
                        return ParseLiteralExpression(out expression);

                    case SyntaxKind.OpenParenToken:
                        return ParseParenthesizedExpression(out expression);

                    default:
                        break;
                }

                expression = null;

                return false;
            }

            bool ParsePrefixUnaryExpression(out ExpressionSyntax expression)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.PlusToken or SyntaxKind.MinusToken)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParsePrefixUnaryOrLiteralOrParenthesizedExpression(out var innerExpression))
                    {
                        expression = new PrefixUnaryExpressionSyntax(token.Kind is SyntaxKind.PlusToken ? SyntaxKind.UnaryPlusExpression : SyntaxKind.UnaryMinusExpression)
                        {
                            OperatorToken = token,
                            Operand = innerExpression
                        };

                        return true;
                    }
                }

                expression = null;

                return false;
            }

            bool ParseLiteralExpression(out ExpressionSyntax expression)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.NumericLiteralToken)
                {
                    tokenListViewer.AdvanceToken();

                    expression = new LiteralExpressionSyntax(SyntaxKind.NumericLiteralExpression)
                    {
                        Token = token
                    };

                    return true;
                }

                expression = null;

                return false;
            }

            bool ParseParenthesizedExpression(out ExpressionSyntax expression)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.OpenParenToken)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParseExpression(out var innerExpression))
                    {
                        var peekToken = tokenListViewer.PeekToken();

                        if (peekToken.Kind is SyntaxKind.CloseParenToken)
                        {
                            tokenListViewer.AdvanceToken();

                            expression = new ParenthesizedExpressionSyntax()
                            {
                                OpenParenToken = token,
                                Expression = innerExpression,
                                CloseParenToken = peekToken
                            };

                            return true;
                        }
                    }
                }

                expression = null;

                return false;
            }
        }
    }

    internal partial class SyntaxParser
    {
        private static Dictionary<SyntaxKind, int> _precedenceOfOperators = new()
        {
            { SyntaxKind.PlusToken, 0 },
            { SyntaxKind.MinusToken, 0 },
            { SyntaxKind.AsteriskToken, 1 },
            { SyntaxKind.SlashToken, 1 },
            { SyntaxKind.PercentToken, 1 }
        };

        private static Dictionary<SyntaxKind, SyntaxKind> _binaryExpressionKindOfOperators = new()
        {
            { SyntaxKind.PlusToken, SyntaxKind.AddExpression },
            { SyntaxKind.MinusToken, SyntaxKind.SubtractExpression },
            { SyntaxKind.AsteriskToken, SyntaxKind.MultiplyExpression },
            { SyntaxKind.SlashToken, SyntaxKind.DivideExpression },
            { SyntaxKind.PercentToken, SyntaxKind.ModuloExpression }
        };
    }
}
