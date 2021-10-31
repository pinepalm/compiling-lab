using System;
using System.Collections.Generic;
using System.Linq;
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
            var scope = new MemberScope();

            foreach (var compilationUnit in _tree.CompilationUnits)
            {
                foreach (var member in compilationUnit.Members)
                {
                    switch (member.Kind)
                    {
                        case SyntaxKind.MethodDeclaration:
                            RealizeMethod(member as MethodDeclarationSyntax, scope);

                            break;
                        default:
                            break;
                    }
                }
            }

            return builder.ToString();

            void RealizeMethod(MethodDeclarationSyntax method, MemberScope scope)
            {
                if (method.Body is null)
                {
                    builder.Append("declare");
                    builder.Append(" ");
                    RealizeType(method.ReturnType, scope);
                    builder.Append(" ");
                    builder.Append($"@{method.Identifier.Value}");

                    scope.Members.Add(($"{method.Identifier.Value}", METHOD), ($"@{method.Identifier.Value}", method));

                    var nextScope = new MemberScope(scope);

                    RealizeParameterList(method.ParameterList, nextScope);

                    builder.Append(_delimiters[((SyntaxToken)(method.SemicolonToken)).Kind]);
                }
                else
                {
                    builder.Append("define dso_local");
                    builder.Append(" ");
                    RealizeType(method.ReturnType, scope);
                    builder.Append(" ");
                    builder.Append($"@{method.Identifier.Value}");

                    scope.Members.Add(($"{method.Identifier.Value}", METHOD), ($"@{method.Identifier.Value}", method));

                    var nextScope = new MemberScope(scope);

                    RealizeParameterList(method.ParameterList, nextScope);

                    RealizeBody(method.Body, nextScope, method.ParameterList.Parameters.Count + 1);
                }
            }

            void RealizeType(TypeSyntax type, MemberScope scope)
            {
                switch (type.Kind)
                {
                    case SyntaxKind.PredefinedType:
                        if (_predefinedTypes.TryGetValue((type as PredefinedTypeSyntax).Keyword.Kind, out var typeCode))
                        {
                            builder.Append(typeCode);
                        }

                        break;
                    default:
                        break;
                }
            }

            void RealizeParameterList(ParameterListSyntax parameterList, MemberScope scope)
            {
                builder.Append($"{_delimiters[parameterList.OpenParenToken.Kind]}");

                for (int i = 0; i < parameterList.Parameters.Count; i++)
                {
                    var parameter = parameterList.Parameters[i];

                    RealizeType(parameter.Type, scope);

                    if (parameter.Identifier is not null)
                    {
                        var identifier = (SyntaxToken)(parameter.Identifier);

                        if (scope.Members.TryGetValue(($"{identifier.Value}", VARIABLE), out var value))
                        {
                            throw new SemanticException();
                        }
                        else
                        {
                            builder.Append(" ");
                            builder.Append($"%{i}");

                            scope.Members.Add(($"{identifier.Value}", VARIABLE), ($"%{i}", parameter));
                        }
                    }

                    if (i != parameterList.Parameters.Count - 1)
                    {
                        builder.Append(", ");
                    }
                }

                builder.Append($"{_delimiters[parameterList.CloseParenToken.Kind]}");
            }

            void RealizeBody(BlockSyntax body, MemberScope scope, int startReg)
            {
                builder.Append(_delimiters[body.OpenBraceToken.Kind]);
                builder.AppendLine();
                RealizeStatements(body.Statements, scope, startReg);
                builder.Append(_delimiters[body.CloseBraceToken.Kind]);
            }

            void RealizeStatements(IReadOnlyList<StatementSyntax> statements, MemberScope scope, int startReg)
            {
                foreach (var statement in statements)
                {
                    switch (statement.Kind)
                    {
                        case SyntaxKind.ReturnStatement:
                            {
                                var returnStatement = statement as ReturnStatementSyntax;

                                RealizeExpressionExcludeAssignment(returnStatement.Expression, scope, startReg, out int? endReg, out _);

                                if (endReg is null)
                                {
                                    throw new SemanticException();
                                }

                                startReg = (int)endReg + 1;

                                builder.Append($"{_directives[returnStatement.ReturnKeyword.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} %{endReg}");
                                builder.Append(_delimiters[returnStatement.SemicolonToken.Kind]);
                            }

                            break;
                        case SyntaxKind.LocalDeclarationStatement:
                            {
                                var localDeclarationStatement = statement as LocalDeclarationStatementSyntax;

                                if (ContainsModifierKind(localDeclarationStatement.Modifiers, SyntaxKind.ConstKeyword))
                                {
                                    foreach (var variable in localDeclarationStatement.Declaration.Variables)
                                    {
                                        if (variable.Initializer is null)
                                        {
                                            throw new SemanticException();
                                        }
                                    }
                                }

                                RealizeVariableDeclaration(localDeclarationStatement.Declaration, scope, startReg, out _, out int lastReg);
                                startReg = lastReg + 1;

                                builder.Append(_delimiters[localDeclarationStatement.SemicolonToken.Kind]);

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

                            break;
                        case SyntaxKind.ExpressionStatement:
                            {
                                var expressionStatement = statement as ExpressionStatementSyntax;

                                RealizeExpression(expressionStatement.Expression, scope, startReg, out _, out int lastReg);
                                startReg = lastReg + 1;

                                builder.Append(_delimiters[expressionStatement.SemicolonToken.Kind]);
                            }

                            break;
                        case SyntaxKind.EmptyStatement:
                            {
                                var emptyStatement = statement as EmptyStatementSyntax;

                                builder.Append(_delimiters[emptyStatement.SemicolonToken.Kind]);
                            }

                            break;
                        default:
                            break;
                    }
                }
            }

            void RealizeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, MemberScope scope, int startReg, out int? endReg, out int lastReg)
            {
                foreach (var variable in variableDeclaration.Variables)
                {
                    var identifier = variable.Identifier;

                    if (scope.Members.TryGetValue(($"{identifier.Value}", VARIABLE), out var value))
                    {
                        throw new SemanticException();
                    }
                    else
                    {
                        builder.Append($"%{startReg} = {_directives[variable.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]}");
                        builder.AppendLine();

                        scope.Members.Add(($"{identifier.Value}", VARIABLE), ($"%{startReg}", variable));
                        startReg++;
                    }
                }

                foreach (var variable in variableDeclaration.Variables)
                {
                    var identifier = variable.Identifier;

                    if (scope.TryLookup(($"{identifier.Value}", VARIABLE), out var value))
                    {
                        if (variable.Initializer is not null)
                        {
                            RealizeExpressionExcludeAssignment(variable.Initializer.Value, scope, startReg, out int? tempEndReg, out _);

                            if (tempEndReg is null)
                            {
                                throw new SemanticException();
                            }

                            builder.Append($"{_directives[variable.Initializer.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} %{tempEndReg}, {_predefinedTypes[SyntaxKind.IntKeyword]}* {value.regName}");
                            builder.AppendLine();

                            startReg = (int)tempEndReg + 1;
                        }
                    }
                    else
                    {
                        throw new SemanticException();
                    }
                }

                endReg = lastReg = startReg - 1;
            }

            void RealizeExpression(ExpressionSyntax expression, MemberScope scope, int startReg, out int? endReg, out int lastReg)
            {
                if (expression.Kind is SyntaxKind.SimpleAssignmentExpression)
                {
                    var simpleAssignmentExpression = expression as AssignmentExpressionSyntax;
                    var identifierName = simpleAssignmentExpression.Left as IdentifierNameSyntax;

                    if (scope.TryLookup(($"{identifierName.Identifier.Value}", VARIABLE), out var value))
                    {
                        RealizeExpressionExcludeAssignment(simpleAssignmentExpression.Right, scope, startReg, out endReg, out lastReg);

                        if (endReg is null)
                        {
                            throw new SemanticException();
                        }

                        builder.Append($"{_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} %{endReg}, {_predefinedTypes[SyntaxKind.IntKeyword]}* {value.regName}");
                        builder.AppendLine();
                    }
                    else
                    {
                        throw new SemanticException();
                    }
                }
                else
                {
                    RealizeExpressionExcludeAssignment(expression, scope, startReg, out endReg, out lastReg);
                }
            }

            void RealizeExpressionExcludeAssignment(ExpressionSyntax expression, MemberScope scope, int startReg, out int? endReg, out int lastReg)
            {
                switch (expression.Kind)
                {
                    case SyntaxKind.UnaryPlusExpression:
                    case SyntaxKind.UnaryMinusExpression:
                        {
                            var prefixUnaryExpression = expression as PrefixUnaryExpressionSyntax;

                            RealizeExpressionExcludeAssignment(prefixUnaryExpression.Operand, scope, startReg, out int? beforeEndReg, out _);

                            if (beforeEndReg is null)
                            {
                                throw new SemanticException();
                            }

                            builder.Append($"%{beforeEndReg + 1} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} 0, %{beforeEndReg}");
                            builder.AppendLine();

                            endReg = lastReg = (int)beforeEndReg + 1;
                        }

                        break;
                    case SyntaxKind.NumericLiteralExpression:
                        {
                            var literalExpression = expression as LiteralExpressionSyntax;

                            builder.Append($"%{startReg} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {literalExpression.Token.Value}");
                            builder.AppendLine();

                            endReg = lastReg = startReg;
                        }

                        break;
                    case SyntaxKind.ParenthesizedExpression:
                        {
                            var parenthesizedExpression = expression as ParenthesizedExpressionSyntax;

                            RealizeExpressionExcludeAssignment(parenthesizedExpression.Expression, scope, startReg, out endReg, out lastReg);
                        }

                        break;
                    case SyntaxKind.InvocationExpression:
                        {
                            var invocationExpression = expression as InvocationExpressionSyntax;
                            var identifierName = invocationExpression.Expression as IdentifierNameSyntax;

                            if (scope.TryLookup(($"{identifierName.Identifier.Value}", METHOD), out var value))
                            {
                                var methodDeclaration = value.node as MethodDeclarationSyntax;

                                if (invocationExpression.ArgumentList.Arguments.Count != methodDeclaration.ParameterList.Parameters.Count)
                                {
                                    throw new SemanticException();
                                }

                                RealizeArgumentList(invocationExpression.ArgumentList, scope, startReg, out int? beforeEndReg, out _, out string[] arguments);

                                if (methodDeclaration.ReturnType is PredefinedTypeSyntax predefinedType &&
                                    predefinedType.Keyword.Kind is SyntaxKind.VoidKeyword)
                                {
                                    builder.Append($"{_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.VoidKeyword]} {value.regName}");
                                    builder.Append($"{_delimiters[invocationExpression.ArgumentList.OpenParenToken.Kind]}");

                                    for (int i = 0; i < arguments.Length; i++)
                                    {
                                        builder.Append(arguments[i]);

                                        if (i != arguments.Length - 1)
                                        {
                                            builder.Append(", ");
                                        }
                                    }

                                    builder.Append($"{_delimiters[invocationExpression.ArgumentList.CloseParenToken.Kind]}");
                                    builder.AppendLine();

                                    lastReg = (int)beforeEndReg;

                                    endReg = null;
                                }
                                else
                                {
                                    builder.Append($"%{beforeEndReg + 1} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} {value.regName}");
                                    builder.Append($"{_delimiters[invocationExpression.ArgumentList.OpenParenToken.Kind]}");

                                    for (int i = 0; i < arguments.Length; i++)
                                    {
                                        builder.Append(arguments[i]);

                                        if (i != arguments.Length - 1)
                                        {
                                            builder.Append(", ");
                                        }
                                    }

                                    builder.Append($"{_delimiters[invocationExpression.ArgumentList.CloseParenToken.Kind]}");
                                    builder.AppendLine();

                                    lastReg = (int)beforeEndReg + 1;

                                    endReg = lastReg;
                                }
                            }
                            else
                            {
                                throw new SemanticException();
                            }
                        }

                        break;
                    case SyntaxKind.IdentifierName:
                        {
                            var identifierName = expression as IdentifierNameSyntax;

                            if (scope.TryLookup(($"{identifierName.Identifier.Value}", VARIABLE), out var value))
                            {
                                builder.Append($"%{startReg} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]}, {_predefinedTypes[SyntaxKind.IntKeyword]}* {value.regName}");
                                builder.AppendLine();

                                endReg = lastReg = startReg;
                            }
                            else
                            {
                                throw new SemanticException();
                            }
                        }

                        break;
                    case SyntaxKind.AddExpression:
                    case SyntaxKind.SubtractExpression:
                    case SyntaxKind.MultiplyExpression:
                    case SyntaxKind.DivideExpression:
                    case SyntaxKind.ModuloExpression:
                        {
                            var binaryExpression = expression as BinaryExpressionSyntax;

                            RealizeExpressionExcludeAssignment(binaryExpression.Left, scope, startReg, out int? middleReg, out _);

                            if (middleReg is null)
                            {
                                throw new SemanticException();
                            }

                            RealizeExpressionExcludeAssignment(binaryExpression.Right, scope, (int)middleReg + 1, out int? beforeEndReg, out _);

                            if (beforeEndReg is null)
                            {
                                throw new SemanticException();
                            }

                            if (expression.Kind is SyntaxKind.ModuloExpression)
                            {
                                builder.Append($"%{beforeEndReg + 1} = {_directives[SyntaxKind.DivideExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %{middleReg}, %{beforeEndReg}");
                                builder.AppendLine();
                                builder.Append($"%{beforeEndReg + 2} = {_directives[SyntaxKind.MultiplyExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %{beforeEndReg + 1}, %{beforeEndReg}");
                                builder.AppendLine();
                                builder.Append($"%{beforeEndReg + 3} = {_directives[SyntaxKind.SubtractExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %{middleReg}, %{beforeEndReg + 2}");
                                builder.AppendLine();

                                endReg = lastReg = (int)beforeEndReg + 3;
                            }
                            else
                            {
                                builder.Append($"%{beforeEndReg + 1} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} %{middleReg}, %{beforeEndReg}");
                                builder.AppendLine();

                                endReg = lastReg = (int)beforeEndReg + 1;
                            }
                        }

                        break;
                    default:
                        endReg = null;
                        lastReg = startReg;

                        break;
                }
            }

            void RealizeArgumentList(ArgumentListSyntax argumentList, MemberScope scope, int startReg, out int? endReg, out int lastReg, out string[] arguments)
            {
                var args = new string[argumentList.Arguments.Count];

                for (int i = 0; i < argumentList.Arguments.Count; i++)
                {
                    var argument = argumentList.Arguments[i];

                    RealizeExpressionExcludeAssignment(argument.Expression, scope, startReg, out int? tempEndReg, out _);

                    if (tempEndReg is null)
                    {
                        throw new SemanticException();
                    }

                    args[i] = $"{_predefinedTypes[SyntaxKind.IntKeyword]} %{tempEndReg}";

                    startReg = (int)tempEndReg + 1;
                }

                endReg = lastReg = startReg - 1;

                arguments = args;
            }
        }
    }

    internal partial class LLVMIRBuilder
    {
        private static readonly Dictionary<SyntaxKind, string> _predefinedTypes = new()
        {
            { SyntaxKind.IntKeyword, "i32" },
            { SyntaxKind.VoidKeyword, "void" }
        };

        private static readonly Dictionary<SyntaxKind, string> _delimiters = new()
        {
            { SyntaxKind.OpenParenToken, "(" },
            { SyntaxKind.CloseParenToken, ")" },
            { SyntaxKind.OpenBraceToken, "{" },
            { SyntaxKind.CloseBraceToken, "}" },
            { SyntaxKind.SemicolonToken, Environment.NewLine }
        };

        private static readonly Dictionary<SyntaxKind, string> _directives = new()
        {
            { SyntaxKind.UnaryPlusExpression, "add" },
            { SyntaxKind.UnaryMinusExpression, "sub" },
            { SyntaxKind.NumericLiteralExpression, "add" },
            { SyntaxKind.AddExpression, "add" },
            { SyntaxKind.SubtractExpression, "sub" },
            { SyntaxKind.MultiplyExpression, "mul" },
            { SyntaxKind.DivideExpression, "sdiv" },
            { SyntaxKind.SimpleAssignmentExpression, "store" },
            { SyntaxKind.EqualsValueClause, "store" },
            { SyntaxKind.InvocationExpression, "call" },
            { SyntaxKind.IdentifierName, "load" },
            { SyntaxKind.ReturnKeyword, "ret" },
            { SyntaxKind.VariableDeclarator, "alloca" }
        };

        private const string METHOD = "method";
        private const string VARIABLE = "variable";
    }
}
