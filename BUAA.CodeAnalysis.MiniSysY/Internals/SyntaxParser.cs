using System;
using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY.Internals
{
    internal class SyntaxParser
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

                        bool ParseExpression(out ExpressionSyntax expression)
                        {
                            var token = tokenListViewer.PeekToken();

                            if (token.Kind is SyntaxKind.NumericLiteralToken)
                            {
                                tokenListViewer.AdvanceToken();

                                expression = new ExpressionSyntax() { NumericLiteralToken = token };

                                return true;
                            }

                            expression = null;

                            return false;
                        }
                    }
                }
            }
        }
    }
}
