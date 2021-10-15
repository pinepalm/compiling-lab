using System;
using System.Collections.Generic;
using System.Text;

namespace BUAA.CodeAnalysis.MiniSysY.Internals
{
    internal partial class LLVMIRBuilder
    {
        private readonly SyntaxTree _tree;

        public LLVMIRBuilder(SyntaxTree tree)
        {
            _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        }

        public string Realize()
        {
            var builder = new StringBuilder();

            foreach (var compilationUnit in _tree.CompilationUnits)
            {
                foreach (var member in compilationUnit.Members)
                {
                    switch (member.Kind)
                    {
                        case SyntaxKind.MethodDeclaration:
                            RealizeMethod(member as MethodDeclarationSyntax);

                            break;
                        default:
                            break;
                    }
                }
            }

            return builder.ToString();

            void RealizeMethod(MethodDeclarationSyntax method)
            {
                if (method is null)
                {
                    return;
                }

                builder.Append("define dso_local");
                builder.Append(" ");
                RealizeReturnType(method.ReturnType);
                RealizeIdentifier(method.IdentifierToken);
                RealizeParameterList(method.ParameterList);
                RealizeBody(method.Body);
                
                void RealizeReturnType(TypeSyntax returnType)
                {
                    switch (returnType.Kind)
                    {
                        case SyntaxKind.PredefinedType:
                            if (_keywords.TryGetValue((returnType as PredefinedTypeSyntax).Keyword.Kind, out var typeCode))
                            {
                                builder.Append(typeCode);
                                builder.Append(" ");
                            }

                            break;
                        default:
                            break;
                    }
                }

                void RealizeIdentifier(SyntaxToken identifier)
                {
                    builder.Append($"@{identifier.Value}");
                }

                void RealizeParameterList(ParameterListSyntax parameterList)
                {
                    builder.Append($"{_delimiters[parameterList.OpenParenToken.Kind]}{_delimiters[parameterList.CloseParenToken.Kind]}");
                }

                void RealizeBody(BlockSyntax body)
                {
                    builder.Append(_delimiters[body.OpenBraceToken.Kind]);
                    builder.AppendLine();
                    RealizeStatements(body.Statements);
                    builder.Append(_delimiters[body.CloseBraceToken.Kind]);

                    void RealizeStatements(IReadOnlyList<StatementSyntax> statements)
                    {
                        foreach (var statement in statements)
                        {
                            switch (statement.Kind)
                            {
                                case SyntaxKind.ReturnStatement:
                                    {
                                        var returnStatement = statement as ReturnStatementSyntax;

                                        builder.Append(_keywords[returnStatement.ReturnKeyword.Kind]);
                                        builder.Append(" ");
                                        RealizeExpression(returnStatement.Expression);
                                        builder.Append(_delimiters[returnStatement.SemicolonToken.Kind]);
                                    }

                                    break;
                                default:
                                    break;
                            }
                        }

                        void RealizeExpression(ExpressionSyntax expression)
                        {
                            builder.Append(_keywords[SyntaxKind.IntKeyword]);
                            builder.Append(" ");
                            builder.Append(expression.NumericLiteralToken.Value);
                        }
                    }
                }
            }
        }
    }

    internal partial class LLVMIRBuilder
    {
        private static Dictionary<SyntaxKind, string> _keywords = new()
        {
            { SyntaxKind.IntKeyword, "i32" },
            { SyntaxKind.ReturnKeyword, "ret" }
        };

        private static Dictionary<SyntaxKind, string> _delimiters = new()
        {
            { SyntaxKind.OpenParenToken, "(" },
            { SyntaxKind.CloseParenToken, ")" },
            { SyntaxKind.OpenBraceToken, "{" },
            { SyntaxKind.CloseBraceToken, "}" },
            { SyntaxKind.SemicolonToken, Environment.NewLine }
        };
    }
}
