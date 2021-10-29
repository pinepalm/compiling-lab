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
            }

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
            }

            void RealizeStatements(IReadOnlyList<StatementSyntax> statements)
            {
                foreach (var statement in statements)
                {
                    switch (statement.Kind)
                    {
                        case SyntaxKind.ReturnStatement:
                            {
                                var returnStatement = statement as ReturnStatementSyntax;

                                RealizeExpression(returnStatement.Expression, 1, out int? endReg);
                                builder.Append($"{_keywords[returnStatement.ReturnKeyword.Kind]} {_keywords[SyntaxKind.IntKeyword]} %{endReg}");
                                builder.Append(_delimiters[returnStatement.SemicolonToken.Kind]);
                            }

                            break;
                        default:
                            break;
                    }
                }
            }

            void RealizeExpression(ExpressionSyntax expression, int startReg, out int? endReg)
            {
                switch (expression.Kind)
                {
                    case SyntaxKind.UnaryPlusExpression:
                    case SyntaxKind.UnaryMinusExpression:
                        {
                            var prefixUnaryExpression = expression as PrefixUnaryExpressionSyntax;

                            RealizeExpression(prefixUnaryExpression.Operand, startReg, out int? beforeEndReg);
                            builder.Append($"%{beforeEndReg + 1} = {_expressionOperators[expression.Kind]} {_keywords[SyntaxKind.IntKeyword]} 0, %{beforeEndReg}");
                            builder.AppendLine();

                            endReg = beforeEndReg + 1;
                        }

                        break;
                    case SyntaxKind.NumericLiteralExpression:
                        {
                            var literalExpression = expression as LiteralExpressionSyntax;

                            builder.Append($"%{startReg} = {_expressionOperators[expression.Kind]} {_keywords[SyntaxKind.IntKeyword]} 0, {literalExpression.Token.Value}");
                            builder.AppendLine();

                            endReg = startReg;
                        }

                        break;
                    case SyntaxKind.ParenthesizedExpression:
                        {
                            var parenthesizedExpression = expression as ParenthesizedExpressionSyntax;

                            RealizeExpression(parenthesizedExpression.Expression, startReg, out endReg);
                        }

                        break;
                    case SyntaxKind.AddExpression:
                    case SyntaxKind.SubtractExpression:
                    case SyntaxKind.MultiplyExpression:
                    case SyntaxKind.DivideExpression:
                    case SyntaxKind.ModuloExpression:
                        {
                            var binaryExpression = expression as BinaryExpressionSyntax;

                            RealizeExpression(binaryExpression.Left, startReg, out int? middleReg);
                            RealizeExpression(binaryExpression.Right, (int)middleReg + 1, out int? beforeEndReg);

                            if (expression.Kind is SyntaxKind.ModuloExpression)
                            {
                                builder.Append($"%{beforeEndReg + 1} = {_expressionOperators[SyntaxKind.DivideExpression]} {_keywords[SyntaxKind.IntKeyword]} %{middleReg}, %{beforeEndReg}");
                                builder.AppendLine();
                                builder.Append($"%{beforeEndReg + 2} = {_expressionOperators[SyntaxKind.MultiplyExpression]} {_keywords[SyntaxKind.IntKeyword]} %{beforeEndReg + 1}, %{beforeEndReg}");
                                builder.AppendLine();
                                builder.Append($"%{beforeEndReg + 3} = {_expressionOperators[SyntaxKind.SubtractExpression]} {_keywords[SyntaxKind.IntKeyword]} %{middleReg}, %{beforeEndReg + 2}");
                                builder.AppendLine();

                                endReg = beforeEndReg + 3;
                            }
                            else
                            {
                                builder.Append($"%{beforeEndReg + 1} = {_expressionOperators[expression.Kind]} {_keywords[SyntaxKind.IntKeyword]} %{middleReg}, %{beforeEndReg}");
                                builder.AppendLine();

                                endReg = beforeEndReg + 1;
                            }
                        }

                        break;
                    default:
                        endReg = null;

                        break;
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

        private static Dictionary<SyntaxKind, string> _expressionOperators = new()
        {
            { SyntaxKind.UnaryPlusExpression, "add" },
            { SyntaxKind.UnaryMinusExpression, "sub" },
            { SyntaxKind.NumericLiteralExpression, "add" },
            { SyntaxKind.AddExpression, "add" },
            { SyntaxKind.SubtractExpression, "sub" },
            { SyntaxKind.MultiplyExpression, "mul" },
            { SyntaxKind.DivideExpression, "sdiv" }
        };
    }
}
