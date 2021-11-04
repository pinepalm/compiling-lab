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
                var token = tokenListViewer.PeekToken();

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
                    ParseBlock(out var body))
                {
                    members.Add(new MethodDeclarationSyntax()
                    {
                        ReturnType = returnType,
                        Identifier = identifier,
                        ParameterList = parameterList,
                        Body = (BlockSyntax)body,
                        SemicolonToken = null
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

                        parameterList = new ParameterListSyntax()
                        {
                            OpenParenToken = token,
                            Parameters = (new List<ParameterSyntax>()).AsReadOnly(),
                            CloseParenToken = peekToken
                        };

                        return true;
                    }
                }

                parameterList = null;

                return false;
            }

            bool ParseBlock(out StatementSyntax statement)
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

                            statement = new BlockSyntax()
                            {
                                OpenBraceToken = token,
                                Statements = statements,
                                CloseBraceToken = endToken
                            };

                            return true;
                        }
                    }
                }

                statement = null;

                return false;
            }

            bool ParseStatement(out StatementSyntax statement)
            {
                int position = tokenListViewer.Position;
                var token = tokenListViewer.PeekToken();

                switch (token.Kind)
                {
                    case SyntaxKind.ReturnKeyword:
                        {
                            if (ParseReturnStatement(out statement))
                            {
                                return true;
                            }
                            else
                            {
                                goto default;
                            }
                        }
                    case SyntaxKind.ConstKeyword:
                    case SyntaxKind.IntKeyword:
                        {
                            if (ParseLocalDeclarationStatement(out statement))
                            {
                                return true;
                            }
                            else
                            {
                                goto default;
                            }
                        }
                    case SyntaxKind.SemicolonToken:
                        {
                            if (ParseEmptyStatement(out statement))
                            {
                                return true;
                            }
                            else
                            {
                                goto default;
                            }
                        }
                    case SyntaxKind.IfKeyword:
                        {
                            if (ParseIfStatement(out statement))
                            {
                                return true;
                            }
                            else
                            {
                                goto default;
                            }
                        }
                    case SyntaxKind.OpenBraceToken:
                        {
                            if (ParseBlock(out statement))
                            {
                                return true;
                            }
                            else
                            {
                                goto default;
                            }
                        }
                    default:
                        {
                            if (ParseExpressionStatement(out statement))
                            {
                                return true;
                            }
                            else
                            {
                                tokenListViewer.Reset(position);

                                statement = null;

                                return false;
                            }
                        }
                }
            }

            bool ParseStatements(out IReadOnlyList<StatementSyntax> statements)
            {
                var statementList = new List<StatementSyntax>();

                while (!tokenListViewer.IsIndexAtEnd)
                {
                    var token = tokenListViewer.PeekToken();

                    if (ParseStatement(out var statement))
                    {
                        statementList.Add(statement);
                    }
                    else
                    {
                        break;
                    }
                }

                statements = statementList;

                return true;
            }

            bool ParseReturnStatement(out StatementSyntax statement)
            {
                int position = tokenListViewer.Position;

                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.ReturnKeyword)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParseExpressionExcludeAssignment(out var expression, _defaultExpressionAllowOperators))
                    {
                        var endToken = tokenListViewer.PeekToken();

                        if (endToken.Kind is SyntaxKind.SemicolonToken)
                        {
                            tokenListViewer.AdvanceToken();

                            statement = new ReturnStatementSyntax()
                            {
                                ReturnKeyword = token,
                                Expression = expression,
                                SemicolonToken = endToken
                            };

                            return true;
                        }
                    }
                }

                tokenListViewer.Reset(position);

                statement = null;

                return false;
            }

            bool ParseLocalDeclarationStatement(out StatementSyntax statement)
            {
                int position = tokenListViewer.Position;

                if (ParseVariableModifiers(out var variableModifiers) &&
                    ParseVariableDeclaration(out var variableDeclaration))
                {
                    var endToken = tokenListViewer.PeekToken();

                    if (endToken.Kind is SyntaxKind.SemicolonToken)
                    {
                        tokenListViewer.AdvanceToken();

                        statement = new LocalDeclarationStatementSyntax()
                        {
                            Modifiers = variableModifiers,
                            Declaration = variableDeclaration,
                            SemicolonToken = endToken
                        };

                        return true;
                    }
                }

                tokenListViewer.Reset(position);

                statement = null;

                return false;
            }

            bool ParseVariableModifiers(out IReadOnlyList<SyntaxToken> variableModifiers)
            {
                var modifiers = new List<SyntaxToken>();

                while (true)
                {
                    var token = tokenListViewer.PeekToken();

                    if (_modifierKinds.Contains(token.Kind) && !ContainsModifierKind(modifiers, token.Kind))
                    {
                        modifiers.Add(token);

                        tokenListViewer.AdvanceToken();
                    }
                    else
                    {
                        break;
                    }
                }

                variableModifiers = modifiers.AsReadOnly();

                return true;
            }

            bool ParseVariableDeclaration(out VariableDeclarationSyntax variableDeclaration)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.IntKeyword)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParseVariableDeclarators(out var variableDeclarators))
                    {
                        variableDeclaration = new VariableDeclarationSyntax()
                        {
                            Type = new PredefinedTypeSyntax()
                            {
                                Keyword = token
                            },
                            Variables = variableDeclarators
                        };

                        return true;
                    }
                }

                variableDeclaration = null;

                return false;
            }

            bool ParseVariableDeclarators(out IReadOnlyList<VariableDeclaratorSyntax> variableDeclarators)
            {
                var declarators = new List<VariableDeclaratorSyntax>();
                int position = tokenListViewer.Position;

                while (true)
                {
                    if (ParseVariableDeclarator(out var variableDeclarator))
                    {
                        position = tokenListViewer.Position;

                        declarators.Add(variableDeclarator);

                        var token = tokenListViewer.PeekToken();

                        if (token.Kind is SyntaxKind.CommaToken)
                        {
                            tokenListViewer.AdvanceToken();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        tokenListViewer.Reset(position);

                        break;
                    }
                }

                if (declarators.Any())
                {
                    variableDeclarators = declarators.AsReadOnly();

                    return true;
                }

                variableDeclarators = null;

                return false;
            }

            bool ParseVariableDeclarator(out VariableDeclaratorSyntax variableDeclarator)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.IdentifierToken)
                {
                    tokenListViewer.AdvanceToken();

                    var peekToken = tokenListViewer.PeekToken();

                    if (peekToken.Kind is SyntaxKind.EqualsToken &&
                        ParseEqualsValueClause(out var equalsValueClause))
                    {
                        variableDeclarator = new VariableDeclaratorSyntax()
                        {
                            Identifier = token,
                            Initializer = equalsValueClause
                        };

                        return true;
                    }
                    else
                    {
                        variableDeclarator = new VariableDeclaratorSyntax()
                        {
                            Identifier = token,
                            Initializer = null
                        };

                        return true;
                    }
                }

                variableDeclarator = null;

                return false;
            }

            bool ParseEqualsValueClause(out EqualsValueClauseSyntax equalsValueClause)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.EqualsToken)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParseExpressionExcludeAssignment(out var expression, _defaultExpressionAllowOperators))
                    {
                        equalsValueClause = new EqualsValueClauseSyntax()
                        {
                            EqualsToken = token,
                            Value = expression
                        };

                        return true;
                    }
                }

                equalsValueClause = null;

                return false;
            }

            bool ParseExpressionStatement(out StatementSyntax statement)
            {
                int position = tokenListViewer.Position;

                if (ParseExpression(out var expression, _defaultExpressionAllowOperators))
                {
                    var endToken = tokenListViewer.PeekToken();

                    if (endToken.Kind is SyntaxKind.SemicolonToken)
                    {
                        tokenListViewer.AdvanceToken();

                        statement = new ExpressionStatementSyntax()
                        {
                            Expression = expression,
                            SemicolonToken = endToken
                        };

                        return true;
                    }
                }

                tokenListViewer.Reset(position);

                statement = null;

                return false;
            }

            bool ParseEmptyStatement(out StatementSyntax statement)
            {
                int position = tokenListViewer.Position;

                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.SemicolonToken)
                {
                    tokenListViewer.AdvanceToken();

                    statement = new EmptyStatementSyntax()
                    {
                        SemicolonToken = token
                    };

                    return true;
                }

                tokenListViewer.Reset(position);

                statement = null;

                return false;
            }

            bool ParseIfStatement(out StatementSyntax statement)
            {
                int position = tokenListViewer.Position;
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.IfKeyword)
                {
                    tokenListViewer.AdvanceToken();

                    var peekToken = tokenListViewer.PeekToken();

                    if (peekToken.Kind is SyntaxKind.OpenParenToken)
                    {
                        tokenListViewer.AdvanceToken();

                        if (ParseExpressionExcludeAssignment(out var expression, _conditionExpressionAllowOperators))
                        {
                            var peekToken1 = tokenListViewer.PeekToken();

                            if (peekToken1.Kind is SyntaxKind.CloseParenToken)
                            {
                                tokenListViewer.AdvanceToken();

                                if (ParseStatement(out var innerStatement))
                                {
                                    var peekToken2 = tokenListViewer.PeekToken();

                                    if (peekToken2.Kind is SyntaxKind.ElseKeyword)
                                    {
                                        if (ParseElseClause(out var elseClause))
                                        {
                                            statement = new IfStatementSyntax()
                                            {
                                                IfKeyword = token,
                                                OpenParenToken = peekToken,
                                                Condition = expression,
                                                CloseParenToken = peekToken1,
                                                Statement = innerStatement,
                                                Else = elseClause
                                            };

                                            return true;
                                        }
                                        else
                                        {
                                            statement = new IfStatementSyntax()
                                            {
                                                IfKeyword = token,
                                                OpenParenToken = peekToken,
                                                Condition = expression,
                                                CloseParenToken = peekToken1,
                                                Statement = innerStatement,
                                                Else = null
                                            };

                                            return true;
                                        }
                                    }
                                    else
                                    {
                                        statement = new IfStatementSyntax()
                                        {
                                            IfKeyword = token,
                                            OpenParenToken = peekToken,
                                            Condition = expression,
                                            CloseParenToken = peekToken1,
                                            Statement = innerStatement,
                                            Else = null
                                        };

                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                tokenListViewer.Reset(position);

                statement = null;

                return false;
            }

            bool ParseElseClause(out ElseClauseSyntax elseClause)
            {
                int position = tokenListViewer.Position;
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.ElseKeyword)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParseStatement(out var statement))
                    {
                        elseClause = new ElseClauseSyntax()
                        {
                            ElseKeyword = token,
                            Statement = statement
                        };

                        return true;
                    }
                }

                tokenListViewer.Reset(position);

                elseClause = null;

                return false;
            }

            bool ParseExpression(out ExpressionSyntax expression, HashSet<SyntaxKind> allowOperators)
            {
                var token = tokenListViewer.PeekToken();
                var peekToken = tokenListViewer.PeekToken(1);

                if (token.Kind is SyntaxKind.IdentifierToken && peekToken.Kind is SyntaxKind.EqualsToken)
                {
                    tokenListViewer.AdvanceToken(2);

                    if (ParseExpressionExcludeAssignment(out var innerExpression, allowOperators))
                    {
                        expression = new AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression)
                        {
                            Left = new IdentifierNameSyntax()
                            {
                                Identifier = token
                            },
                            OperatorToken = peekToken,
                            Right = innerExpression
                        };

                        return true;
                    }
                }
                else
                {
                    if (ParseExpressionExcludeAssignment(out var innerExpression, allowOperators))
                    {
                        expression = innerExpression;

                        return true;
                    }
                }

                expression = null;

                return false;
            }

            bool ParseExpressionExcludeAssignment(out ExpressionSyntax expression, HashSet<SyntaxKind> allowOperators)
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

                var samePrecedenceExpressionList = new List<ExpressionSyntax>();
                var operatorList = new List<SyntaxToken>();
                int position = tokenListViewer.Position;

                while (ParseExpressionExcludeAssignmentCore(out var samePrecedenceExpression, allowOperators))
                {
                    samePrecedenceExpressionList.Add(samePrecedenceExpression);

                    var token = tokenListViewer.PeekToken();

                    if (allowOperators.Contains(token.Kind) &&
                        _precedenceOfOperators.ContainsKey(token.Kind))
                    {
                        position = tokenListViewer.Position;

                        tokenListViewer.AdvanceToken();

                        operatorList.Add(token);
                    }
                    else
                    {
                        break;
                    }
                }

                if (samePrecedenceExpressionList.Any())
                {
                    if (samePrecedenceExpressionList.Count == operatorList.Count)
                    {
                        operatorList.RemoveAt(operatorList.Count - 1);
                        tokenListViewer.Reset(position);
                    }

                    // while (operatorLinkedList.Any())
                    // {
                    //     var leftExpression = samePrecedenceExpressionLinkedList.First();
                    //     samePrecedenceExpressionLinkedList.RemoveFirst();

                    //     var @operator = operatorLinkedList.First();
                    //     operatorLinkedList.RemoveFirst();

                    //     var rightExpression = samePrecedenceExpressionLinkedList.First();
                    //     samePrecedenceExpressionLinkedList.RemoveFirst();

                    //     var binaryExpression = new BinaryExpressionSyntax(_binaryExpressionKindOfOperators[@operator.Kind])
                    //     {
                    //         Left = leftExpression,
                    //         OperatorToken = @operator,
                    //         Right = rightExpression
                    //     };

                    //     samePrecedenceExpressionLinkedList.AddFirst(binaryExpression);
                    // }

                    // expression = samePrecedenceExpressionLinkedList.First();

                    expression = MergeExpressions(samePrecedenceExpressionList, operatorList);

                    return true;
                }

                expression = null;

                return false;

                ExpressionSyntax MergeExpressions(IReadOnlyList<ExpressionSyntax> expressions, IReadOnlyList<SyntaxToken> operators)
                {
                    if (!operators.Any())
                    {
                        return expressions.First();
                    }

                    var samePrecedenceMergedExpressionList = new List<ExpressionSyntax>();
                    var samePrecedenceExpressionLinkedList = new LinkedList<ExpressionSyntax>();
                    var operatorMergedList = new List<SyntaxToken>();
                    var operatorLinkedList = new LinkedList<SyntaxToken>();
                    int position = 0;
                    int? precedence = null;

                    for (int i = 0; i < expressions.Count;)
                    {
                        samePrecedenceExpressionLinkedList.AddLast(expressions[i]);

                        var token = (i < expressions.Count - 1) ? operators[i] : SyntaxToken.Empty;

                        if (_precedenceOfOperators.TryGetValue(token.Kind, out var precedenceOfOperator))
                        {
                            if (precedence is null || precedence == precedenceOfOperator)
                            {
                                position = i;

                                i++;

                                operatorLinkedList.AddLast(token);

                                precedence = precedenceOfOperator;
                            }
                            else if (precedence > precedenceOfOperator)
                            {
                                samePrecedenceMergedExpressionList.Add(MergeExpressionsCore(samePrecedenceExpressionLinkedList, operatorLinkedList));
                                samePrecedenceExpressionLinkedList = new LinkedList<ExpressionSyntax>();
                                operatorLinkedList = new LinkedList<SyntaxToken>();

                                i++;

                                operatorMergedList.Add(token);

                                precedence = null;
                            }
                            else
                            {
                                samePrecedenceExpressionLinkedList.RemoveLast();
                                operatorLinkedList.RemoveLast();
                                i = position;

                                token = operators[i];

                                samePrecedenceMergedExpressionList.Add(MergeExpressionsCore(samePrecedenceExpressionLinkedList, operatorLinkedList));
                                samePrecedenceExpressionLinkedList = new LinkedList<ExpressionSyntax>();
                                operatorLinkedList = new LinkedList<SyntaxToken>();

                                i++;

                                operatorMergedList.Add(token);

                                precedence = null;
                            }
                        }
                        else
                        {
                            samePrecedenceMergedExpressionList.Add(MergeExpressionsCore(samePrecedenceExpressionLinkedList, operatorLinkedList));

                            break;
                        }
                    }

                    return MergeExpressions(samePrecedenceMergedExpressionList, operatorMergedList);

                    ExpressionSyntax MergeExpressionsCore(LinkedList<ExpressionSyntax> expressionLinkedList, LinkedList<SyntaxToken> operatorLinkedList)
                    {
                        while (operatorLinkedList.Any())
                        {
                            var leftExpression = expressionLinkedList.First();
                            expressionLinkedList.RemoveFirst();

                            var @operator = operatorLinkedList.First();
                            operatorLinkedList.RemoveFirst();

                            var rightExpression = expressionLinkedList.First();
                            expressionLinkedList.RemoveFirst();

                            var binaryExpression = new BinaryExpressionSyntax(_binaryExpressionKindOfOperators[@operator.Kind])
                            {
                                Left = leftExpression,
                                OperatorToken = @operator,
                                Right = rightExpression
                            };

                            expressionLinkedList.AddFirst(binaryExpression);
                        }

                        return expressionLinkedList.First();
                    }
                }
            }

            bool ParseExpressionExcludeAssignmentCore(out ExpressionSyntax expression, HashSet<SyntaxKind> allowOperators)
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

                var primitiveExpressionLinkedList = new LinkedList<ExpressionSyntax>();
                var operatorLinkedList = new LinkedList<SyntaxToken>();
                int position = tokenListViewer.Position;
                int? precedence = null;

                while (ParsePrimitiveExpression(out var primitiveExpression, allowOperators))
                {
                    primitiveExpressionLinkedList.AddLast(primitiveExpression);

                    var token = tokenListViewer.PeekToken();

                    if (allowOperators.Contains(token.Kind) &&
                        _precedenceOfOperators.TryGetValue(token.Kind, out var precedenceOfOperator))
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
                            primitiveExpressionLinkedList.RemoveLast();
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

                if (primitiveExpressionLinkedList.Any())
                {
                    while (operatorLinkedList.Any())
                    {
                        var leftExpression = primitiveExpressionLinkedList.First();
                        primitiveExpressionLinkedList.RemoveFirst();

                        var @operator = operatorLinkedList.First();
                        operatorLinkedList.RemoveFirst();

                        var rightExpression = primitiveExpressionLinkedList.First();
                        primitiveExpressionLinkedList.RemoveFirst();

                        var binaryExpression = new BinaryExpressionSyntax(_binaryExpressionKindOfOperators[@operator.Kind])
                        {
                            Left = leftExpression,
                            OperatorToken = @operator,
                            Right = rightExpression
                        };

                        primitiveExpressionLinkedList.AddFirst(binaryExpression);
                    }

                    expression = primitiveExpressionLinkedList.First();

                    return true;
                }

                expression = null;

                return false;
            }

            bool ParsePrimitiveExpression(out ExpressionSyntax expression, HashSet<SyntaxKind> allowOperators)
            {
                var token = tokenListViewer.PeekToken();

                switch (token.Kind)
                {
                    case SyntaxKind.ExclamationToken:
                    case SyntaxKind.PlusToken:
                    case SyntaxKind.MinusToken:
                        return ParsePrefixUnaryExpression(out expression, allowOperators);

                    case SyntaxKind.NumericLiteralToken:
                        return ParseLiteralExpression(out expression, allowOperators);

                    case SyntaxKind.OpenParenToken:
                        return ParseParenthesizedExpression(out expression, allowOperators);

                    case SyntaxKind.IdentifierToken:
                        return ParseIdentifierNameOrInvocationExpression(out expression, allowOperators);

                    default:
                        break;
                }

                expression = null;

                return false;
            }

            bool ParsePrefixUnaryExpression(out ExpressionSyntax expression, HashSet<SyntaxKind> allowOperators)
            {
                var token = tokenListViewer.PeekToken();

                if (allowOperators.Contains(token.Kind) &&
                    _prefixUnaryExpressionKindOfOperators.ContainsKey(token.Kind))
                {
                    tokenListViewer.AdvanceToken();

                    if (ParsePrimitiveExpression(out var innerExpression, allowOperators))
                    {
                        expression = new PrefixUnaryExpressionSyntax(_prefixUnaryExpressionKindOfOperators[token.Kind])
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

            bool ParseLiteralExpression(out ExpressionSyntax expression, HashSet<SyntaxKind> allowOperators)
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

            bool ParseParenthesizedExpression(out ExpressionSyntax expression, HashSet<SyntaxKind> allowOperators)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.OpenParenToken)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParseExpressionExcludeAssignment(out var innerExpression, allowOperators))
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

            bool ParseIdentifierNameOrInvocationExpression(out ExpressionSyntax expression, HashSet<SyntaxKind> allowOperators)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.IdentifierToken)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParseArgumentList(out var argumentList, allowOperators))
                    {
                        expression = new InvocationExpressionSyntax()
                        {
                            Expression = new IdentifierNameSyntax()
                            {
                                Identifier = token
                            },
                            ArgumentList = argumentList
                        };

                        return true;
                    }
                    else
                    {
                        expression = new IdentifierNameSyntax()
                        {
                            Identifier = token
                        };

                        return true;
                    }
                }

                expression = null;

                return false;
            }

            bool ParseArgumentList(out ArgumentListSyntax argumentList, HashSet<SyntaxKind> allowOperators)
            {
                var token = tokenListViewer.PeekToken();

                if (token.Kind is SyntaxKind.OpenParenToken)
                {
                    tokenListViewer.AdvanceToken();

                    if (ParseArguments(out var arguments, allowOperators))
                    {
                        var peekToken = tokenListViewer.PeekToken();

                        if (peekToken.Kind is SyntaxKind.CloseParenToken)
                        {
                            tokenListViewer.AdvanceToken();

                            argumentList = new ArgumentListSyntax()
                            {
                                OpenParenToken = token,
                                Arguments = arguments,
                                CloseParenToken = peekToken
                            };

                            return true;
                        }
                    }
                }

                argumentList = null;

                return false;
            }

            bool ParseArguments(out IReadOnlyList<ArgumentSyntax> arguments, HashSet<SyntaxKind> allowOperators)
            {
                var argumentList = new List<ArgumentSyntax>();
                int position = tokenListViewer.Position;

                while (true)
                {
                    if (ParseExpressionExcludeAssignment(out var expression, allowOperators))
                    {
                        position = tokenListViewer.Position;

                        argumentList.Add(new ArgumentSyntax()
                        {
                            Expression = expression
                        });

                        var token = tokenListViewer.PeekToken();

                        if (token.Kind is SyntaxKind.CommaToken)
                        {
                            tokenListViewer.AdvanceToken();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        tokenListViewer.Reset(position);

                        break;
                    }
                }

                arguments = argumentList.AsReadOnly();

                return true;
            }

            bool ContainsModifierKind(IEnumerable<SyntaxToken> modifiers, SyntaxKind kind)
            {
                foreach (var modifier in modifiers)
                {
                    if (kind == modifier.Kind)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    internal partial class SyntaxParser
    {
        private static readonly Dictionary<SyntaxKind, int> _precedenceOfOperators = new()
        {
            { SyntaxKind.BarBarToken, 0 },

            { SyntaxKind.AmpersandAmpersandToken, 1 },

            { SyntaxKind.ExclamationEqualsToken, 2 },
            { SyntaxKind.EqualsEqualsToken, 2 },

            { SyntaxKind.GreaterThanEqualsToken, 3 },
            { SyntaxKind.LessThanEqualsToken, 3 },
            { SyntaxKind.GreaterThanToken, 3 },
            { SyntaxKind.LessThanToken, 3 },

            { SyntaxKind.PlusToken, 4 },
            { SyntaxKind.MinusToken, 4 },

            { SyntaxKind.AsteriskToken, 5 },
            { SyntaxKind.SlashToken, 5 },
            { SyntaxKind.PercentToken, 5 }
        };

        private static readonly Dictionary<SyntaxKind, SyntaxKind> _binaryExpressionKindOfOperators = new()
        {
            { SyntaxKind.BarBarToken, SyntaxKind.LogicalOrExpression },

            { SyntaxKind.AmpersandAmpersandToken, SyntaxKind.LogicalAndExpression },

            { SyntaxKind.ExclamationEqualsToken, SyntaxKind.NotEqualsExpression },
            { SyntaxKind.EqualsEqualsToken, SyntaxKind.EqualsExpression },

            { SyntaxKind.GreaterThanEqualsToken, SyntaxKind.GreaterThanOrEqualExpression },
            { SyntaxKind.LessThanEqualsToken, SyntaxKind.LessThanOrEqualExpression },
            { SyntaxKind.GreaterThanToken, SyntaxKind.GreaterThanExpression },
            { SyntaxKind.LessThanToken, SyntaxKind.LessThanExpression },

            { SyntaxKind.PlusToken, SyntaxKind.AddExpression },
            { SyntaxKind.MinusToken, SyntaxKind.SubtractExpression },

            { SyntaxKind.AsteriskToken, SyntaxKind.MultiplyExpression },
            { SyntaxKind.SlashToken, SyntaxKind.DivideExpression },
            { SyntaxKind.PercentToken, SyntaxKind.ModuloExpression }
        };

        private static readonly Dictionary<SyntaxKind, SyntaxKind> _prefixUnaryExpressionKindOfOperators = new()
        {
            { SyntaxKind.ExclamationToken, SyntaxKind.LogicalNotExpression },

            { SyntaxKind.PlusToken, SyntaxKind.UnaryPlusExpression },
            { SyntaxKind.MinusToken, SyntaxKind.UnaryMinusExpression }
        };

        private static readonly HashSet<SyntaxKind> _modifierKinds = new()
        {
            SyntaxKind.ConstKeyword
        };

        private static readonly HashSet<SyntaxKind> _defaultExpressionAllowOperators = new()
        {
            SyntaxKind.PlusToken,
            SyntaxKind.MinusToken,

            SyntaxKind.AsteriskToken,
            SyntaxKind.SlashToken,
            SyntaxKind.PercentToken
        };

        private static readonly HashSet<SyntaxKind> _conditionExpressionAllowOperators = new()
        {
            SyntaxKind.BarBarToken,

            SyntaxKind.AmpersandAmpersandToken,

            SyntaxKind.ExclamationEqualsToken,
            SyntaxKind.EqualsEqualsToken,

            SyntaxKind.GreaterThanEqualsToken,
            SyntaxKind.LessThanEqualsToken,
            SyntaxKind.GreaterThanToken,
            SyntaxKind.LessThanToken,

            SyntaxKind.ExclamationToken,

            SyntaxKind.PlusToken,
            SyntaxKind.MinusToken,

            SyntaxKind.AsteriskToken,
            SyntaxKind.SlashToken,
            SyntaxKind.PercentToken
        };
    }
}
