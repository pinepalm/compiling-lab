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
                builder.Append("declare void @memset(i32*, i32, i32)");
                builder.AppendLine();

                foreach (var member in compilationUnit.Members)
                {
                    switch (member.Kind)
                    {
                        case SyntaxKind.FieldDeclaration:
                            RealizeField(member as FieldDeclarationSyntax, scope);

                            break;
                        case SyntaxKind.MethodDeclaration:
                            RealizeMethod(member as MethodDeclarationSyntax, scope);

                            break;
                        default:
                            break;
                    }
                }

                if (!scope.Members.TryGetValue((MAIN, METHOD), out var value) ||
                    ((value.Node as MethodDeclarationSyntax).ReturnType as PredefinedTypeSyntax).Keyword.Kind is not SyntaxKind.IntKeyword)
                {
                    throw new SemanticException();
                }
            }

            return builder.ToString();

            void RealizeField(FieldDeclarationSyntax field, MemberScope scope)
            {
                if (ContainsModifierKind(field.Modifiers, SyntaxKind.ConstKeyword))
                {
                    foreach (var variable in field.Declaration.Variables)
                    {
                        if (variable.Initializer is null)
                        {
                            throw new SemanticException();
                        }
                    }
                }

                var variableDeclaration = field.Declaration;

                foreach (var variable in variableDeclaration.Variables)
                {
                    var identifier = variable.Identifier;

                    if (scope.Members.TryGetValue(($"{identifier.Value}", VARIABLE), out _) ||
                        scope.Members.TryGetValue(($"{identifier.Value}", METHOD), out _))
                    {
                        throw new SemanticException();
                    }
                    else
                    {
                        if (variable.RankSpecifiers?.Any() ?? false)
                        {
                            int[] ranks = new int[variable.RankSpecifiers.Count];

                            for (int i = 0; i < variable.RankSpecifiers.Count; i++)
                            {
                                var rankSpecifier = variable.RankSpecifiers[i];
                                int rank = CalculateExpressionExcludeAssignment(rankSpecifier.Size, scope);

                                if (rank < 0)
                                {
                                    throw new SemanticException();
                                }

                                ranks[i] = rank;
                            }

                            builder.Append($"@{identifier.Value} = dso_local global");
                            builder.Append(" ");
                            RealizeFieldDeclarationArrayInitializer(variable.Initializer?.Value, scope, variableDeclaration.Type, ranks);
                            builder.AppendLine();

                            scope.Members.Add(($"{identifier.Value}", VARIABLE), new MemberInfo()
                            {
                                Name = $"{identifier.Value}",
                                ActualName = $"@{identifier.Value}",
                                Kind = VARIABLE,
                                Node = field
                            }.WithProperty(RANKS, ranks)
                            .WithProperty(UNIT_ACTUAL_NAME, $"@{identifier.Value}"));
                        }
                        else
                        {
                            int value = variable.Initializer is not null ? CalculateExpressionExcludeAssignment(variable.Initializer.Value, scope) : 0;

                            builder.Append($"@{identifier.Value} = dso_local global {_predefinedTypes[SyntaxKind.IntKeyword]} {value}");
                            builder.AppendLine();

                            scope.Members.Add(($"{identifier.Value}", VARIABLE), new MemberInfo()
                            {
                                Name = $"{identifier.Value}",
                                ActualName = $"@{identifier.Value}",
                                Kind = VARIABLE,
                                Node = field
                            }.WithProperty(UNIT_ACTUAL_NAME, $"@{identifier.Value}"));
                        }
                    }
                }
            }

            void RealizeFieldDeclarationArrayType(int[] ranks, MemberScope scope, TypeSyntax baseType, int index)
            {
                if (index >= ranks.Length)
                {
                    RealizeType(baseType, scope);

                    return;
                }

                int rank = ranks[index];

                builder.Append($"[{rank} x ");
                RealizeFieldDeclarationArrayType(ranks, scope, baseType, index + 1);
                builder.Append("]");
            }

            void RealizeFieldDeclarationArrayInitializer(ExpressionSyntax expression, MemberScope scope, TypeSyntax baseType, int[] ranks)
            {
                RealizeFieldDeclarationArrayInitializerCore(expression, scope, baseType, ranks, 0);
            }

            void RealizeFieldDeclarationArrayInitializerCore(ExpressionSyntax expression, MemberScope scope, TypeSyntax baseType, int[] ranks, int index)
            {
                if (index >= ranks.Length)
                {
                    return;
                }

                int rank = ranks[index];

                RealizeFieldDeclarationArrayType(ranks, scope, baseType, index);
                builder.Append(" ");

                if (expression is null)
                {
                    builder.Append("zeroinitializer");

                    return;
                }

                builder.Append("[");

                if (index < ranks.Length - 1)
                {
                    var initializerExpression = expression as InitializerExpressionSyntax;
                    var expressions = initializerExpression.Expressions;

                    if (rank < expressions.Count)
                    {
                        throw new SemanticException();
                    }

                    for (int i = 0; i < rank; i++)
                    {
                        RealizeFieldDeclarationArrayInitializerCore(i < expressions.Count ? expressions[i] : null, scope, baseType, ranks, index + 1);

                        if (i != rank - 1)
                        {
                            builder.Append(", ");
                        }
                    }
                }
                else
                {
                    var initializerExpression = expression as InitializerExpressionSyntax;
                    var expressions = initializerExpression.Expressions;

                    if (rank < expressions.Count)
                    {
                        throw new SemanticException();
                    }

                    for (int i = 0; i < rank; i++)
                    {
                        int value = i < expressions.Count ? CalculateExpressionExcludeAssignment(expressions[i], scope) : 0;

                        RealizeType(baseType, scope);
                        builder.Append(" ");
                        builder.Append(value);

                        if (i != rank - 1)
                        {
                            builder.Append(", ");
                        }
                    }
                }

                builder.Append("]");
            }

            void RealizeMethod(MethodDeclarationSyntax method, MemberScope scope)
            {
                if (method.Body is null)
                {
                    builder.Append("declare");
                    builder.Append(" ");
                    RealizeType(method.ReturnType, scope);
                    builder.Append(" ");

                    if (scope.Members.TryGetValue(($"{method.Identifier.Value}", VARIABLE), out _) ||
                        scope.Members.TryGetValue(($"{method.Identifier.Value}", METHOD), out _))
                    {
                        throw new SemanticException();
                    }
                    else
                    {
                        builder.Append($"@{method.Identifier.Value}");

                        scope.Members.Add(($"{method.Identifier.Value}", METHOD), new MemberInfo()
                        {
                            Name = $"{method.Identifier.Value}",
                            ActualName = $"@{method.Identifier.Value}",
                            Kind = METHOD,
                            Node = method
                        }.WithProperty(UNIT_ACTUAL_NAME, $"@{method.Identifier.Value}"));
                    }

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

                    if (scope.Members.TryGetValue(($"{method.Identifier.Value}", VARIABLE), out _) ||
                        scope.Members.TryGetValue(($"{method.Identifier.Value}", METHOD), out _))
                    {
                        throw new SemanticException();
                    }
                    else
                    {
                        builder.Append($"@{method.Identifier.Value}");

                        scope.Members.Add(($"{method.Identifier.Value}", METHOD), new MemberInfo()
                        {
                            Name = $"{method.Identifier.Value}",
                            ActualName = $"@{method.Identifier.Value}",
                            Kind = METHOD,
                            Node = method
                        }.WithProperty(UNIT_ACTUAL_NAME, $"@{method.Identifier.Value}"));
                    }

                    var nextScope = new MemberScope(scope);

                    RealizeParameterList(method.ParameterList, nextScope);

                    RealizeBody(method.Body, nextScope, method, method.ParameterList.Parameters.Count + 1);
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

            void RealizeParameterArrayType(int[] ranks, MemberScope scope, TypeSyntax baseType)
            {
                RealizeParameterArrayTypeCore(ranks, scope, baseType, 0);
            }

            void RealizeParameterArrayTypeCore(int[] ranks, MemberScope scope, TypeSyntax baseType, int index)
            {
                if (index >= ranks.Length)
                {
                    RealizeType(baseType, scope);

                    return;
                }

                if (index is 0)
                {
                    RealizeParameterArrayTypeCore(ranks, scope, baseType, index + 1);
                    builder.Append("*");
                }
                else
                {
                    int rank = ranks[index];

                    builder.Append($"[{rank} x ");
                    RealizeParameterArrayTypeCore(ranks, scope, baseType, index + 1);
                    builder.Append("]");
                }
            }

            void RealizeParameterList(ParameterListSyntax parameterList, MemberScope scope)
            {
                builder.Append($"{_delimiters[parameterList.OpenParenToken.Kind]}");

                for (int i = 0; i < parameterList.Parameters.Count; i++)
                {
                    var parameter = parameterList.Parameters[i];

                    if (parameter.RankSpecifiers?.Any() ?? false)
                    {
                        int[] ranks = new int[parameter.RankSpecifiers.Count];

                        for (int j = 0; j < parameter.RankSpecifiers.Count; j++)
                        {
                            var rankSpecifier = parameter.RankSpecifiers[j];

                            if (rankSpecifier.Size is null)
                            {
                                ranks[j] = -1;
                            }
                            else
                            {
                                int rank = CalculateExpressionExcludeAssignment(rankSpecifier.Size, scope);

                                if (rank < 0)
                                {
                                    throw new SemanticException();
                                }

                                ranks[j] = rank;
                            }
                        }

                        RealizeParameterArrayType(ranks, scope, parameter.Type);

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
                                builder.Append($"%r{i}");

                                scope.Members.Add(($"{identifier.Value}", VARIABLE), new MemberInfo()
                                {
                                    Name = $"{identifier.Value}",
                                    ActualName = $"%r{i}",
                                    Kind = VARIABLE,
                                    Node = parameter
                                }.WithProperty(RANKS, ranks)
                                .WithProperty(UNIT_ACTUAL_NAME, $"%r{i}"));
                            }
                        }
                    }
                    else
                    {
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
                                builder.Append($"%r{i}");

                                scope.Members.Add(($"{identifier.Value}", VARIABLE), new MemberInfo()
                                {
                                    Name = $"{identifier.Value}",
                                    ActualName = $"%r{i}",
                                    Kind = VARIABLE,
                                    Node = parameter
                                }.WithProperty(UNIT_ACTUAL_NAME, $"%r{i}"));
                            }
                        }
                    }

                    if (i != parameterList.Parameters.Count - 1)
                    {
                        builder.Append(", ");
                    }
                }

                builder.Append($"{_delimiters[parameterList.CloseParenToken.Kind]}");
            }

            void RealizeBody(BlockSyntax body, MemberScope scope, MethodDeclarationSyntax method, int startReg)
            {
                builder.Append(_delimiters[body.OpenBraceToken.Kind]);
                builder.AppendLine();

                foreach (var parameter in method.ParameterList.Parameters)
                {
                    var identifier = (SyntaxToken)(parameter.Identifier);
                    var value = scope.Members[($"{identifier.Value}", VARIABLE)];

                    if (parameter.RankSpecifiers?.Any() ?? false)
                    {
                        int[] ranks = value.Properties[RANKS] as int[];

                        builder.Append($"%r{startReg} = {_directives[SyntaxKind.VariableDeclarator]} {GenerateParameterArrayType(ranks, scope, parameter.Type, 0)}");
                        builder.AppendLine();
                        builder.Append($"{_directives[SyntaxKind.EqualsValueClause]} {GenerateParameterArrayType(ranks, scope, parameter.Type, 0)} {value.Properties[UNIT_ACTUAL_NAME]}, {GenerateParameterArrayType(ranks, scope, parameter.Type, 0)} * %r{startReg}");
                        builder.AppendLine();

                        value.Properties[UNIT_ACTUAL_NAME] = $"%r{startReg}";
                        startReg++;
                    }
                    else
                    {
                        builder.Append($"%r{startReg} = {_directives[SyntaxKind.VariableDeclarator]} {GenerateType(parameter.Type, scope)}");
                        builder.AppendLine();
                        builder.Append($"{_directives[SyntaxKind.EqualsValueClause]} {GenerateType(parameter.Type, scope)} {value.Properties[UNIT_ACTUAL_NAME]}, {GenerateType(parameter.Type, scope)}* %r{startReg}");
                        builder.AppendLine();

                        value.Properties[UNIT_ACTUAL_NAME] = $"%r{startReg}";
                        startReg++;
                    }
                }

                RealizeStatements(body.Statements, scope, method, null, null, startReg, out _, out _);

                if ((method.ReturnType as PredefinedTypeSyntax).Keyword.Kind is SyntaxKind.VoidKeyword)
                {
                    builder.Append($"{_directives[SyntaxKind.ReturnKeyword]} {_predefinedTypes[SyntaxKind.VoidKeyword]}");
                    builder.AppendLine();
                }
                // "ret i32 0" may be a solution 
                else if ((method.ReturnType as PredefinedTypeSyntax).Keyword.Kind is SyntaxKind.IntKeyword)
                {
                    builder.Append($"{_directives[SyntaxKind.ReturnKeyword]} {_predefinedTypes[SyntaxKind.IntKeyword]} 0");
                    builder.AppendLine();
                }

                builder.Append(_delimiters[body.CloseBraceToken.Kind]);
                builder.AppendLine();
            }

            void RealizeLocalDeclarationArrayType(int[] ranks, MemberScope scope, TypeSyntax baseType, int index)
            {
                if (index >= ranks.Length)
                {
                    RealizeType(baseType, scope);

                    return;
                }

                int rank = ranks[index];

                builder.Append($"[{rank} x ");
                RealizeLocalDeclarationArrayType(ranks, scope, baseType, index + 1);
                builder.Append("]");
            }

            void RealizeLocalDeclarationArrayInitializer(ExpressionSyntax expression, MemberScope scope, TypeSyntax baseType, int[] ranks, string arrayName, bool mustConst, int startReg, out int? endReg, out int lastReg)
            {
                RealizeLocalDeclarationArrayInitializerCore(expression, scope, baseType, ranks, 0, arrayName, mustConst, startReg, out endReg, out lastReg);
            }

            void RealizeLocalDeclarationArrayInitializerCore(ExpressionSyntax expression, MemberScope scope, TypeSyntax baseType, int[] ranks, int index, string arrayName, bool mustConst, int startReg, out int? endReg, out int lastReg)
            {
                if (index >= ranks.Length)
                {
                    endReg = lastReg = startReg - 1;

                    return;
                }

                if (expression is null)
                {
                    builder.Append($"%r{startReg} = getelementptr");
                    builder.Append(" ");
                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, index);
                    builder.Append(", ");
                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, index);
                    builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} 0");
                    builder.AppendLine();

                    startReg++;

                    int elements = ranks[index];

                    for (int i = index + 1; i < ranks.Length; i++)
                    {
                        elements *= ranks[i];

                        builder.Append($"%r{startReg} = getelementptr");
                        builder.Append(" ");
                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, i);
                        builder.Append(", ");
                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, i);
                        builder.Append($"* %r{startReg - 1}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} 0");
                        builder.AppendLine();

                        startReg++;
                    }

                    builder.Append($"call void @memset(i32* %r{startReg - 1}, i32 0, i32 {4 * elements})");
                    builder.AppendLine();

                    endReg = lastReg = startReg - 1;

                    return;
                }

                int rank = ranks[index];

                if (index < ranks.Length - 1)
                {
                    var initializerExpression = expression as InitializerExpressionSyntax;
                    var expressions = initializerExpression.Expressions;

                    if (rank < expressions.Count)
                    {
                        throw new SemanticException();
                    }

                    for (int i = 0; i < rank; i++)
                    {
                        builder.Append($"%r{startReg} = getelementptr");
                        builder.Append(" ");
                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, index);
                        builder.Append(", ");
                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, index);
                        builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} {i}");
                        builder.AppendLine();

                        RealizeLocalDeclarationArrayInitializerCore(i < expressions.Count ? expressions[i] : null, scope, baseType, ranks, index + 1, $"%r{startReg}", mustConst, startReg + 1, out int? beforeEndReg, out _);

                        startReg = (int)beforeEndReg + 1;
                    }

                    endReg = lastReg = startReg - 1;
                }
                else
                {
                    var initializerExpression = expression as InitializerExpressionSyntax;
                    var expressions = initializerExpression.Expressions;

                    if (rank < expressions.Count)
                    {
                        throw new SemanticException();
                    }

                    for (int i = 0; i < rank; i++)
                    {
                        if (i < expressions.Count)
                        {
                            RealizeExpressionExcludeAssignment(expressions[i], scope, mustConst, startReg, out int? beforeEndReg, out _);

                            if (beforeEndReg is null)
                            {
                                throw new SemanticException();
                            }

                            builder.Append($"%r{beforeEndReg + 1} = getelementptr");
                            builder.Append(" ");
                            RealizeLocalDeclarationArrayType(ranks, scope, baseType, index);
                            builder.Append(", ");
                            RealizeLocalDeclarationArrayType(ranks, scope, baseType, index);
                            builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} {i}");
                            builder.AppendLine();

                            builder.Append(_directives[SyntaxKind.SimpleAssignmentExpression]);
                            builder.Append(" ");
                            RealizeLocalDeclarationArrayType(ranks, scope, baseType, index + 1);
                            builder.Append(" ");
                            builder.Append($"%r{beforeEndReg}");
                            builder.Append(", ");
                            RealizeLocalDeclarationArrayType(ranks, scope, baseType, index + 1);
                            builder.Append($"* %r{beforeEndReg + 1}");
                            builder.AppendLine();

                            startReg = (int)beforeEndReg + 2;
                        }
                        else
                        {
                            builder.Append($"%r{startReg} = getelementptr");
                            builder.Append(" ");
                            RealizeLocalDeclarationArrayType(ranks, scope, baseType, index);
                            builder.Append(", ");
                            RealizeLocalDeclarationArrayType(ranks, scope, baseType, index);
                            builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} {i}");
                            builder.AppendLine();

                            builder.Append(_directives[SyntaxKind.EqualsValueClause]);
                            builder.Append(" ");
                            RealizeLocalDeclarationArrayType(ranks, scope, baseType, index + 1);
                            builder.Append(" ");
                            builder.Append("0");
                            builder.Append(", ");
                            RealizeLocalDeclarationArrayType(ranks, scope, baseType, index + 1);
                            builder.Append($"* %r{startReg}");
                            builder.AppendLine();

                            startReg++;
                        }
                    }

                    endReg = lastReg = startReg - 1;
                }
            }

            void RealizeStatement(StatementSyntax statement, MemberScope scope, MethodDeclarationSyntax method, string loopStartTag, string loopEndTag, int startReg, out int? endReg, out int lastReg)
            {
                switch (statement.Kind)
                {
                    case SyntaxKind.ReturnStatement:
                        {
                            var returnStatement = statement as ReturnStatementSyntax;

                            if ((method.ReturnType as PredefinedTypeSyntax).Keyword.Kind is SyntaxKind.IntKeyword)
                            {
                                if (returnStatement.Expression is null)
                                {
                                    throw new SemanticException();
                                }

                                RealizeExpressionExcludeAssignment(returnStatement.Expression, scope, false, startReg, out endReg, out lastReg);

                                if (endReg is null)
                                {
                                    throw new SemanticException();
                                }

                                endReg = lastReg;

                                builder.Append($"{_directives[returnStatement.ReturnKeyword.Kind]} {_predefinedTypes[(method.ReturnType as PredefinedTypeSyntax).Keyword.Kind]} %r{endReg}");
                                builder.Append(_delimiters[returnStatement.SemicolonToken.Kind]);
                            }
                            else if ((method.ReturnType as PredefinedTypeSyntax).Keyword.Kind is SyntaxKind.VoidKeyword)
                            {
                                if (returnStatement.Expression is not null)
                                {
                                    throw new SemanticException();
                                }

                                endReg = lastReg = startReg - 1;

                                builder.Append($"{_directives[returnStatement.ReturnKeyword.Kind]} {_predefinedTypes[(method.ReturnType as PredefinedTypeSyntax).Keyword.Kind]}");
                                builder.Append(_delimiters[returnStatement.SemicolonToken.Kind]);
                            }
                            else
                            {
                                throw new SemanticException();
                            }
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

                            var variableDeclaration = localDeclarationStatement.Declaration;

                            foreach (var variable in variableDeclaration.Variables)
                            {
                                var identifier = variable.Identifier;

                                if (scope.Members.TryGetValue(($"{identifier.Value}", VARIABLE), out _))
                                {
                                    throw new SemanticException();
                                }
                                else
                                {
                                    if (variable.RankSpecifiers?.Any() ?? false)
                                    {
                                        int[] ranks = new int[variable.RankSpecifiers.Count];

                                        for (int i = 0; i < variable.RankSpecifiers.Count; i++)
                                        {
                                            var rankSpecifier = variable.RankSpecifiers[i];
                                            int rank = CalculateExpressionExcludeAssignment(rankSpecifier.Size, scope);

                                            if (rank < 0)
                                            {
                                                throw new SemanticException();
                                            }

                                            ranks[i] = rank;
                                        }

                                        builder.Append($"%r{startReg} = {_directives[variable.Kind]}");
                                        builder.Append(" ");
                                        RealizeLocalDeclarationArrayType(ranks, scope, variableDeclaration.Type, 0);
                                        builder.AppendLine();

                                        scope.Members.Add(($"{identifier.Value}", VARIABLE), new MemberInfo()
                                        {
                                            Name = $"{identifier.Value}",
                                            ActualName = $"%r{startReg}",
                                            Kind = VARIABLE,
                                            Node = localDeclarationStatement
                                        }.WithProperty(RANKS, ranks)
                                        .WithProperty(UNIT_ACTUAL_NAME, $"%r{startReg}"));
                                        startReg++;

                                        if (scope.TryLookup(($"{identifier.Value}", VARIABLE), out var value))
                                        {
                                            if (variable.Initializer is not null)
                                            {
                                                bool mustConst = HasConst(value.Node);

                                                RealizeLocalDeclarationArrayInitializer(variable.Initializer.Value, scope, variableDeclaration.Type, ranks, value.Properties[UNIT_ACTUAL_NAME] as string, mustConst, startReg, out int? tempEndReg, out _);
                                                startReg = (int)tempEndReg + 1;
                                            }
                                        }
                                        else
                                        {
                                            throw new SemanticException();
                                        }
                                    }
                                    else
                                    {
                                        builder.Append($"%r{startReg} = {_directives[variable.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]}");
                                        builder.AppendLine();

                                        scope.Members.Add(($"{identifier.Value}", VARIABLE), new MemberInfo()
                                        {
                                            Name = $"{identifier.Value}",
                                            ActualName = $"%r{startReg}",
                                            Kind = VARIABLE,
                                            Node = localDeclarationStatement
                                        }.WithProperty(UNIT_ACTUAL_NAME, $"%r{startReg}"));
                                        startReg++;

                                        if (scope.TryLookup(($"{identifier.Value}", VARIABLE), out var value))
                                        {
                                            if (variable.Initializer is not null)
                                            {
                                                bool mustConst = HasConst(value.Node);

                                                RealizeExpressionExcludeAssignment(variable.Initializer.Value, scope, mustConst, startReg, out int? tempEndReg, out _);

                                                if (tempEndReg is null)
                                                {
                                                    throw new SemanticException();
                                                }

                                                builder.Append($"{_directives[variable.Initializer.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg}, {_predefinedTypes[SyntaxKind.IntKeyword]}* {value.Properties[UNIT_ACTUAL_NAME]}");
                                                builder.AppendLine();

                                                startReg = (int)tempEndReg + 1;
                                            }
                                        }
                                        else
                                        {
                                            throw new SemanticException();
                                        }
                                    }
                                }
                            }

                            endReg = lastReg = startReg - 1;

                            builder.Append(_delimiters[localDeclarationStatement.SemicolonToken.Kind]);
                        }

                        break;
                    case SyntaxKind.ExpressionStatement:
                        {
                            var expressionStatement = statement as ExpressionStatementSyntax;

                            RealizeExpression(expressionStatement.Expression, scope, startReg, out endReg, out lastReg);
                            endReg = lastReg;

                            builder.Append(_delimiters[expressionStatement.SemicolonToken.Kind]);
                        }

                        break;
                    case SyntaxKind.EmptyStatement:
                        {
                            var emptyStatement = statement as EmptyStatementSyntax;

                            endReg = lastReg = startReg - 1;

                            builder.Append(_delimiters[emptyStatement.SemicolonToken.Kind]);
                        }

                        break;
                    case SyntaxKind.IfStatement:
                        {
                            var ifStatement = statement as IfStatementSyntax;

                            RealizeExpressionExcludeAssignment(ifStatement.Condition, scope, false, startReg, out int? beforeEndReg, out int beforeLastReg);

                            if (beforeEndReg is null)
                            {
                                throw new SemanticException();
                            }

                            builder.Append($"%r{beforeEndReg + 1} = {_directives[SyntaxKind.NotEqualsExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{beforeEndReg}, 0");
                            builder.AppendLine();

                            if (ifStatement.Else is null)
                            {
                                builder.Append($"br i1 %r{beforeEndReg + 1}, label %r{beforeEndReg + 2}, label %r{beforeEndReg + 3}");
                                builder.AppendLine();
                                builder.Append($"r{beforeEndReg + 2}:");
                                builder.AppendLine();

                                RealizeStatement(ifStatement.Statement, scope, method, loopStartTag, loopEndTag, (int)beforeEndReg + 5, out _, out lastReg);

                                builder.Append($"br label %r{beforeEndReg + 3}");
                                builder.AppendLine();

                                builder.Append($"r{beforeEndReg + 3}:");
                                builder.AppendLine();
                                // nop
                                builder.Append($"%r{beforeEndReg + 4} = {_directives[SyntaxKind.AddExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} 0, 0");
                                builder.AppendLine();

                                endReg = lastReg;
                            }
                            else
                            {
                                builder.Append($"br i1 %r{beforeEndReg + 1}, label %r{beforeEndReg + 2}, label %r{beforeEndReg + 3}");
                                builder.AppendLine();
                                builder.Append($"r{beforeEndReg + 2}:");
                                builder.AppendLine();

                                RealizeStatement(ifStatement.Statement, scope, method, loopStartTag, loopEndTag, (int)beforeEndReg + 6, out _, out int middleReg);

                                builder.Append($"br label %r{beforeEndReg + 4}");
                                builder.AppendLine();

                                builder.Append($"r{beforeEndReg + 3}:");
                                builder.AppendLine();

                                RealizeStatement(ifStatement.Else.Statement, scope, method, loopStartTag, loopEndTag, middleReg + 1, out _, out lastReg);

                                builder.Append($"br label %r{beforeEndReg + 4}");
                                builder.AppendLine();

                                builder.Append($"r{beforeEndReg + 4}:");
                                builder.AppendLine();
                                // nop
                                builder.Append($"%r{beforeEndReg + 5} = {_directives[SyntaxKind.AddExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} 0, 0");
                                builder.AppendLine();

                                endReg = lastReg;
                            }
                        }

                        break;
                    case SyntaxKind.Block:
                        {
                            var block = statement as BlockSyntax;

                            var nextScope = new MemberScope(scope);

                            RealizeStatements(block.Statements, nextScope, method, loopStartTag, loopEndTag, startReg, out endReg, out lastReg);

                            endReg = lastReg;
                        }

                        break;
                    case SyntaxKind.WhileStatement:
                        {
                            var whileStatement = statement as WhileStatementSyntax;

                            builder.Append($"br label %r{startReg}");
                            builder.AppendLine();
                            builder.Append($"r{startReg}:");
                            builder.AppendLine();

                            RealizeExpressionExcludeAssignment(whileStatement.Condition, scope, false, startReg + 1, out int? beforeEndReg, out int beforeLastReg);

                            if (beforeEndReg is null)
                            {
                                throw new SemanticException();
                            }

                            builder.Append($"%r{beforeEndReg + 1} = {_directives[SyntaxKind.NotEqualsExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{beforeEndReg}, 0");
                            builder.AppendLine();

                            builder.Append($"br i1 %r{beforeEndReg + 1}, label %r{beforeEndReg + 2}, label %r{beforeEndReg + 3}");
                            builder.AppendLine();
                            builder.Append($"r{beforeEndReg + 2}:");
                            builder.AppendLine();

                            RealizeStatement(whileStatement.Statement, scope, method, $"%r{startReg}", $"%r{beforeEndReg + 3}", (int)beforeEndReg + 5, out _, out lastReg);

                            builder.Append($"br label %r{startReg}");
                            builder.AppendLine();

                            builder.Append($"r{beforeEndReg + 3}:");
                            builder.AppendLine();
                            // nop
                            builder.Append($"%r{beforeEndReg + 4} = {_directives[SyntaxKind.AddExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} 0, 0");
                            builder.AppendLine();

                            endReg = lastReg;
                        }

                        break;
                    case SyntaxKind.BreakStatement:
                        {
                            var breakStatement = statement as BreakStatementSyntax;

                            if (loopEndTag is null)
                            {
                                throw new SemanticException();
                            }

                            builder.Append($"br label {loopEndTag}");
                            builder.AppendLine();

                            endReg = lastReg = startReg - 1;
                        }

                        break;
                    case SyntaxKind.ContinueStatement:
                        {
                            var continueStatement = statement as ContinueStatementSyntax;

                            if (loopStartTag is null)
                            {
                                throw new SemanticException();
                            }

                            builder.Append($"br label {loopStartTag}");
                            builder.AppendLine();

                            endReg = lastReg = startReg - 1;
                        }

                        break;
                    default:
                        endReg = lastReg = startReg - 1;

                        break;
                }
            }

            void RealizeStatements(IReadOnlyList<StatementSyntax> statements, MemberScope scope, MethodDeclarationSyntax method, string loopStartTag, string loopEndTag, int startReg, out int? endReg, out int lastReg)
            {
                foreach (var statement in statements)
                {
                    RealizeStatement(statement, scope, method, loopStartTag, loopEndTag, startReg, out _, out int tempLastReg);
                    startReg = tempLastReg + 1;
                }

                endReg = lastReg = startReg - 1;
            }

            void RealizeExpression(ExpressionSyntax expression, MemberScope scope, int startReg, out int? endReg, out int lastReg)
            {
                if (expression.Kind is SyntaxKind.SimpleAssignmentExpression)
                {
                    var simpleAssignmentExpression = expression as AssignmentExpressionSyntax;

                    if (simpleAssignmentExpression.Left.Kind is SyntaxKind.IdentifierName)
                    {
                        var identifierName = simpleAssignmentExpression.Left as IdentifierNameSyntax;

                        if (scope.TryLookup(($"{identifierName.Identifier.Value}", VARIABLE), out var value) &&
                            !HasConst(value.Node))
                        {
                            RealizeExpressionExcludeAssignment(simpleAssignmentExpression.Right, scope, false, startReg, out endReg, out lastReg);

                            if (endReg is null)
                            {
                                throw new SemanticException();
                            }

                            builder.Append($"{_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{endReg}, {_predefinedTypes[SyntaxKind.IntKeyword]}* {value.Properties[UNIT_ACTUAL_NAME]}");
                            builder.AppendLine();
                        }
                        else
                        {
                            throw new SemanticException();
                        }
                    }
                    else if (simpleAssignmentExpression.Left.Kind is SyntaxKind.ElementAccessExpression)
                    {
                        var elementAccessExpression = simpleAssignmentExpression.Left as ElementAccessExpressionSyntax;
                        var identifierName = elementAccessExpression.Expression as IdentifierNameSyntax;

                        if (scope.TryLookup(($"{identifierName.Identifier.Value}", VARIABLE), out var value) &&
                            !HasConst(value.Node))
                        {
                            var baseType = (value.Node.Kind is SyntaxKind.LocalDeclarationStatement ?
                                        (value.Node as LocalDeclarationStatementSyntax).Declaration.Type :
                                        (value.Node.Kind is SyntaxKind.FieldDeclaration ?
                                        (value.Node as FieldDeclarationSyntax).Declaration.Type :
                                        (value.Node as ParameterSyntax).Type));
                            int[] ranks = value.Properties[RANKS] as int[];

                            if (elementAccessExpression.Arguments.Count != ranks.Length)
                            {
                                throw new SemanticException();
                            }

                            RealizeExpressionExcludeAssignment(simpleAssignmentExpression.Right, scope, false, startReg, out int? beforeEndReg, out _);

                            if (beforeEndReg is null)
                            {
                                throw new SemanticException();
                            }

                            startReg = (int)beforeEndReg + 1;

                            string arrayName = value.Properties[UNIT_ACTUAL_NAME] as string;

                            if (value.Node.Kind is SyntaxKind.LocalDeclarationStatement or SyntaxKind.FieldDeclaration)
                            {
                                for (int i = 0; i < elementAccessExpression.Arguments.Count; i++)
                                {
                                    var argument = elementAccessExpression.Arguments[i];

                                    RealizeExpressionExcludeAssignment(argument.Argument.Expression, scope, false, startReg, out int? tempEndReg, out _);

                                    if (tempEndReg is null)
                                    {
                                        throw new SemanticException();
                                    }

                                    builder.Append($"%r{tempEndReg + 1} = getelementptr");
                                    builder.Append(" ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, i);
                                    builder.Append(", ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, i);
                                    builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg}");
                                    builder.AppendLine();

                                    arrayName = $"%r{tempEndReg + 1}";
                                    startReg = (int)tempEndReg + 2;
                                }
                            }
                            else
                            {
                                var argument = elementAccessExpression.Arguments[0];

                                RealizeExpressionExcludeAssignment(argument.Argument.Expression, scope, false, startReg, out int? tempEndReg, out _);

                                if (tempEndReg is null)
                                {
                                    throw new SemanticException();
                                }

                                builder.Append($"%r{tempEndReg + 1} = {_directives[SyntaxKind.IdentifierName]} {GenerateParameterArrayType(ranks, scope, baseType, 0)}, {GenerateParameterArrayType(ranks, scope, baseType, 0)} * {arrayName}");
                                builder.AppendLine();

                                builder.Append($"%r{tempEndReg + 2} = getelementptr");
                                builder.Append(" ");
                                RealizeLocalDeclarationArrayType(ranks, scope, baseType, 1);
                                builder.Append(", ");
                                RealizeLocalDeclarationArrayType(ranks, scope, baseType, 1);
                                builder.Append($"* %r{tempEndReg + 1}, {_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg}");
                                builder.AppendLine();

                                arrayName = $"%r{tempEndReg + 2}";
                                startReg = (int)tempEndReg + 3;

                                for (int i = 1; i < elementAccessExpression.Arguments.Count; i++)
                                {
                                    argument = elementAccessExpression.Arguments[i];

                                    RealizeExpressionExcludeAssignment(argument.Argument.Expression, scope, false, startReg, out int? tempEndReg1, out _);

                                    if (tempEndReg1 is null)
                                    {
                                        throw new SemanticException();
                                    }

                                    builder.Append($"%r{tempEndReg1 + 1} = getelementptr");
                                    builder.Append(" ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, i);
                                    builder.Append(", ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, i);
                                    builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg1}");
                                    builder.AppendLine();

                                    arrayName = $"%r{tempEndReg1 + 1}";
                                    startReg = (int)tempEndReg1 + 2;
                                }
                            }

                            builder.Append($"{_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{beforeEndReg}, {_predefinedTypes[SyntaxKind.IntKeyword]}* {arrayName}");
                            builder.AppendLine();

                            endReg = lastReg = startReg - 1;
                        }
                        else
                        {
                            throw new SemanticException();
                        }
                    }
                    else
                    {
                        throw new SemanticException();
                    }
                }
                else
                {
                    RealizeExpressionExcludeAssignment(expression, scope, false, startReg, out endReg, out lastReg);
                }
            }

            void RealizeExpressionExcludeAssignment(ExpressionSyntax expression, MemberScope scope, bool mustConst, int startReg, out int? endReg, out int lastReg)
            {
                switch (expression.Kind)
                {
                    case SyntaxKind.LogicalNotExpression:
                    case SyntaxKind.UnaryPlusExpression:
                    case SyntaxKind.UnaryMinusExpression:
                        {
                            var prefixUnaryExpression = expression as PrefixUnaryExpressionSyntax;

                            RealizeExpressionExcludeAssignment(prefixUnaryExpression.Operand, scope, mustConst, startReg, out int? beforeEndReg, out _);

                            if (beforeEndReg is null)
                            {
                                throw new SemanticException();
                            }

                            if (expression.Kind is SyntaxKind.LogicalNotExpression)
                            {
                                builder.Append($"%r{beforeEndReg + 1} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{beforeEndReg}, 0");
                                builder.AppendLine();
                                builder.Append($"%r{beforeEndReg + 2} = zext i1 %r{beforeEndReg + 1} to {_predefinedTypes[SyntaxKind.IntKeyword]}");
                                builder.AppendLine();

                                endReg = lastReg = (int)beforeEndReg + 2;
                            }
                            else
                            {
                                builder.Append($"%r{beforeEndReg + 1} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} 0, %r{beforeEndReg}");
                                builder.AppendLine();

                                endReg = lastReg = (int)beforeEndReg + 1;
                            }
                        }

                        break;
                    case SyntaxKind.NumericLiteralExpression:
                        {
                            var literalExpression = expression as LiteralExpressionSyntax;

                            builder.Append($"%r{startReg} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {literalExpression.Token.Value}");
                            builder.AppendLine();

                            endReg = lastReg = startReg;
                        }

                        break;
                    case SyntaxKind.ParenthesizedExpression:
                        {
                            var parenthesizedExpression = expression as ParenthesizedExpressionSyntax;

                            RealizeExpressionExcludeAssignment(parenthesizedExpression.Expression, scope, mustConst, startReg, out endReg, out lastReg);
                        }

                        break;
                    case SyntaxKind.InvocationExpression:
                        {
                            if (mustConst)
                            {
                                throw new SemanticException();
                            }

                            var invocationExpression = expression as InvocationExpressionSyntax;
                            var identifierName = invocationExpression.Expression as IdentifierNameSyntax;

                            if (scope.TryLookup(($"{identifierName.Identifier.Value}", METHOD), out var value))
                            {
                                var methodDeclaration = value.Node as MethodDeclarationSyntax;

                                if (invocationExpression.ArgumentList.Arguments.Count != methodDeclaration.ParameterList.Parameters.Count)
                                {
                                    throw new SemanticException();
                                }

                                RealizeArgumentList(invocationExpression.ArgumentList, scope, methodDeclaration.ParameterList, startReg, out int? beforeEndReg, out _, out string[] arguments);

                                if (methodDeclaration.ReturnType is PredefinedTypeSyntax predefinedType &&
                                    predefinedType.Keyword.Kind is SyntaxKind.VoidKeyword)
                                {
                                    builder.Append($"{_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.VoidKeyword]} {value.Properties[UNIT_ACTUAL_NAME]}");
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
                                    builder.Append($"%r{beforeEndReg + 1} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} {value.Properties[UNIT_ACTUAL_NAME]}");
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
                                if (mustConst)
                                {
                                    if (!HasConst(value.Node))
                                    {
                                        throw new SemanticException();
                                    }
                                }

                                builder.Append($"%r{startReg} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]}, {_predefinedTypes[SyntaxKind.IntKeyword]}* {value.Properties[UNIT_ACTUAL_NAME]}");
                                builder.AppendLine();

                                endReg = lastReg = startReg;
                            }
                            else
                            {
                                throw new SemanticException();
                            }
                        }

                        break;
                    case SyntaxKind.ElementAccessExpression:
                        {
                            if (mustConst)
                            {
                                throw new SemanticException();
                            }

                            var elementAccessExpression = expression as ElementAccessExpressionSyntax;
                            var identifierName = elementAccessExpression.Expression as IdentifierNameSyntax;

                            if (scope.TryLookup(($"{identifierName.Identifier.Value}", VARIABLE), out var value))
                            {
                                var baseType = (value.Node.Kind is SyntaxKind.LocalDeclarationStatement ?
                                        (value.Node as LocalDeclarationStatementSyntax).Declaration.Type :
                                        (value.Node.Kind is SyntaxKind.FieldDeclaration ?
                                        (value.Node as FieldDeclarationSyntax).Declaration.Type :
                                        (value.Node as ParameterSyntax).Type));
                                int[] ranks = value.Properties[RANKS] as int[];

                                if (elementAccessExpression.Arguments.Count != ranks.Length)
                                {
                                    throw new SemanticException();
                                }

                                string arrayName = value.Properties[UNIT_ACTUAL_NAME] as string;

                                if (value.Node.Kind is SyntaxKind.LocalDeclarationStatement or SyntaxKind.FieldDeclaration)
                                {
                                    for (int i = 0; i < elementAccessExpression.Arguments.Count; i++)
                                    {
                                        var argument = elementAccessExpression.Arguments[i];

                                        RealizeExpressionExcludeAssignment(argument.Argument.Expression, scope, mustConst, startReg, out int? tempEndReg, out _);

                                        if (tempEndReg is null)
                                        {
                                            throw new SemanticException();
                                        }

                                        builder.Append($"%r{tempEndReg + 1} = getelementptr");
                                        builder.Append(" ");
                                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, i);
                                        builder.Append(", ");
                                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, i);
                                        builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg}");
                                        builder.AppendLine();

                                        arrayName = $"%r{tempEndReg + 1}";
                                        startReg = (int)tempEndReg + 2;
                                    }
                                }
                                else
                                {
                                    var argument = elementAccessExpression.Arguments[0];

                                    RealizeExpressionExcludeAssignment(argument.Argument.Expression, scope, mustConst, startReg, out int? tempEndReg, out _);

                                    if (tempEndReg is null)
                                    {
                                        throw new SemanticException();
                                    }

                                    builder.Append($"%r{tempEndReg + 1} = {_directives[SyntaxKind.IdentifierName]} {GenerateParameterArrayType(ranks, scope, baseType, 0)}, {GenerateParameterArrayType(ranks, scope, baseType, 0)} * {arrayName}");
                                    builder.AppendLine();

                                    builder.Append($"%r{tempEndReg + 2} = getelementptr");
                                    builder.Append(" ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, 1);
                                    builder.Append(", ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, 1);
                                    builder.Append($"* %r{tempEndReg + 1}, {_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg}");
                                    builder.AppendLine();

                                    arrayName = $"%r{tempEndReg + 2}";
                                    startReg = (int)tempEndReg + 3;

                                    for (int i = 1; i < elementAccessExpression.Arguments.Count; i++)
                                    {
                                        argument = elementAccessExpression.Arguments[i];

                                        RealizeExpressionExcludeAssignment(argument.Argument.Expression, scope, mustConst, startReg, out int? tempEndReg1, out _);

                                        if (tempEndReg1 is null)
                                        {
                                            throw new SemanticException();
                                        }

                                        builder.Append($"%r{tempEndReg1 + 1} = getelementptr");
                                        builder.Append(" ");
                                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, i);
                                        builder.Append(", ");
                                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, i);
                                        builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg1}");
                                        builder.AppendLine();

                                        arrayName = $"%r{tempEndReg1 + 1}";
                                        startReg = (int)tempEndReg1 + 2;
                                    }
                                }

                                builder.Append($"%r{startReg} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]}, {_predefinedTypes[SyntaxKind.IntKeyword]}* {arrayName}");
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
                    // case SyntaxKind.LogicalOrExpression:
                    // case SyntaxKind.LogicalAndExpression:
                    case SyntaxKind.EqualsExpression:
                    case SyntaxKind.NotEqualsExpression:
                    case SyntaxKind.LessThanExpression:
                    case SyntaxKind.LessThanOrEqualExpression:
                    case SyntaxKind.GreaterThanExpression:
                    case SyntaxKind.GreaterThanOrEqualExpression:
                        {
                            var binaryExpression = expression as BinaryExpressionSyntax;

                            RealizeExpressionExcludeAssignment(binaryExpression.Left, scope, mustConst, startReg, out int? middleReg, out _);

                            if (middleReg is null)
                            {
                                throw new SemanticException();
                            }

                            RealizeExpressionExcludeAssignment(binaryExpression.Right, scope, mustConst, (int)middleReg + 1, out int? beforeEndReg, out _);

                            if (beforeEndReg is null)
                            {
                                throw new SemanticException();
                            }

                            if (expression.Kind is SyntaxKind.ModuloExpression)
                            {
                                builder.Append($"%r{beforeEndReg + 1} = {_directives[SyntaxKind.DivideExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{middleReg}, %r{beforeEndReg}");
                                builder.AppendLine();
                                builder.Append($"%r{beforeEndReg + 2} = {_directives[SyntaxKind.MultiplyExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{beforeEndReg + 1}, %r{beforeEndReg}");
                                builder.AppendLine();
                                builder.Append($"%r{beforeEndReg + 3} = {_directives[SyntaxKind.SubtractExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{middleReg}, %r{beforeEndReg + 2}");
                                builder.AppendLine();

                                endReg = lastReg = (int)beforeEndReg + 3;
                            }
                            else if (expression.Kind is
                                    SyntaxKind.AddExpression or
                                    SyntaxKind.SubtractExpression or
                                    SyntaxKind.MultiplyExpression or
                                    SyntaxKind.DivideExpression)
                            {
                                builder.Append($"%r{beforeEndReg + 1} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{middleReg}, %r{beforeEndReg}");
                                builder.AppendLine();

                                endReg = lastReg = (int)beforeEndReg + 1;
                            }
                            else if (expression.Kind is
                                    SyntaxKind.LogicalOrExpression or
                                    SyntaxKind.LogicalAndExpression)
                            {
                                builder.Append($"%r{beforeEndReg + 1} = {_directives[SyntaxKind.NotEqualsExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{middleReg}, 0");
                                builder.AppendLine();
                                builder.Append($"%r{beforeEndReg + 2} = {_directives[SyntaxKind.NotEqualsExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{beforeEndReg}, 0");
                                builder.AppendLine();
                                builder.Append($"%r{beforeEndReg + 3} = {_directives[expression.Kind]} i1 %r{beforeEndReg + 1}, %r{beforeEndReg + 2}");
                                builder.AppendLine();
                                builder.Append($"%r{beforeEndReg + 4} = zext i1 %r{beforeEndReg + 3} to {_predefinedTypes[SyntaxKind.IntKeyword]}");
                                builder.AppendLine();

                                endReg = lastReg = (int)beforeEndReg + 4;
                            }
                            else
                            {
                                builder.Append($"%r{beforeEndReg + 1} = {_directives[expression.Kind]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{middleReg}, %r{beforeEndReg}");
                                builder.AppendLine();
                                builder.Append($"%r{beforeEndReg + 2} = zext i1 %r{beforeEndReg + 1} to {_predefinedTypes[SyntaxKind.IntKeyword]}");
                                builder.AppendLine();

                                endReg = lastReg = (int)beforeEndReg + 2;
                            }
                        }

                        break;
                    case SyntaxKind.LogicalOrExpression:
                    case SyntaxKind.LogicalAndExpression:
                        {
                            var binaryExpression = expression as BinaryExpressionSyntax;

                            RealizeExpressionExcludeAssignment(binaryExpression.Left, scope, mustConst, startReg, out int? middleReg, out _);

                            if (middleReg is null)
                            {
                                throw new SemanticException();
                            }

                            builder.Append($"%r{middleReg + 1} = {_directives[expression.Kind is SyntaxKind.LogicalOrExpression ? SyntaxKind.EqualsExpression : SyntaxKind.NotEqualsExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{middleReg}, 0");
                            builder.AppendLine();

                            builder.Append($"br i1 %r{middleReg + 1}, label %r{middleReg + 2}, label %r{middleReg + 3}");
                            builder.AppendLine();
                            builder.Append($"r{middleReg + 2}:");
                            builder.AppendLine();

                            RealizeExpressionExcludeAssignment(binaryExpression.Right, scope, mustConst, (int)middleReg + 5, out int? beforeEndReg, out _);

                            if (beforeEndReg is null)
                            {
                                throw new SemanticException();
                            }

                            builder.Append($"br label %r{middleReg + 4}");
                            builder.AppendLine();

                            builder.Append($"r{middleReg + 3}:");
                            builder.AppendLine();

                            builder.Append($"br label %r{middleReg + 4}");
                            builder.AppendLine();

                            builder.Append($"r{middleReg + 4}:");
                            builder.AppendLine();

                            builder.Append($"%r{beforeEndReg + 1} = phi {_predefinedTypes[SyntaxKind.IntKeyword]} [%r{beforeEndReg}, %r{middleReg + 2}], [{(expression.Kind is SyntaxKind.LogicalOrExpression ? 1 : 0)}, %r{middleReg + 3}]");
                            builder.AppendLine();

                            builder.Append($"%r{beforeEndReg + 2} = {_directives[SyntaxKind.NotEqualsExpression]} {_predefinedTypes[SyntaxKind.IntKeyword]} %r{beforeEndReg + 1}, 0");
                            builder.AppendLine();

                            builder.Append($"%r{beforeEndReg + 3} = zext i1 %r{beforeEndReg + 2} to {_predefinedTypes[SyntaxKind.IntKeyword]}");
                            builder.AppendLine();

                            endReg = lastReg = (int)beforeEndReg + 3;
                        }

                        break;
                    default:
                        endReg = null;
                        lastReg = startReg;

                        break;
                }
            }

            void RealizeArgumentList(ArgumentListSyntax argumentList, MemberScope scope, ParameterListSyntax parameterList, int startReg, out int? endReg, out int lastReg, out string[] arguments)
            {
                var args = new string[argumentList.Arguments.Count];

                for (int i = 0; i < argumentList.Arguments.Count; i++)
                {
                    var argument = argumentList.Arguments[i];
                    var parameter = parameterList.Parameters[i];

                    if (parameter.RankSpecifiers?.Any() ?? false)
                    {
                        if (argument.Expression.Kind is SyntaxKind.ElementAccessExpression)
                        {
                            var elementAccessExpression = argument.Expression as ElementAccessExpressionSyntax;
                            var identifierName = elementAccessExpression.Expression as IdentifierNameSyntax;

                            if (scope.TryLookup(($"{identifierName.Identifier.Value}", VARIABLE), out var value))
                            {
                                var baseType = (value.Node.Kind is SyntaxKind.LocalDeclarationStatement ?
                                        (value.Node as LocalDeclarationStatementSyntax).Declaration.Type :
                                        (value.Node.Kind is SyntaxKind.FieldDeclaration ?
                                        (value.Node as FieldDeclarationSyntax).Declaration.Type :
                                        (value.Node as ParameterSyntax).Type));
                                int[] ranks = value.Properties[RANKS] as int[];

                                if (elementAccessExpression.Arguments.Count + parameter.RankSpecifiers.Count != ranks.Length)
                                {
                                    throw new SemanticException();
                                }

                                string arrayName = value.Properties[UNIT_ACTUAL_NAME] as string;

                                if (value.Node.Kind is SyntaxKind.LocalDeclarationStatement or SyntaxKind.FieldDeclaration)
                                {
                                    for (int j = 0; j < elementAccessExpression.Arguments.Count; j++)
                                    {
                                        var argument1 = elementAccessExpression.Arguments[j];

                                        RealizeExpressionExcludeAssignment(argument1.Argument.Expression, scope, false, startReg, out int? tempEndReg, out _);

                                        if (tempEndReg is null)
                                        {
                                            throw new SemanticException();
                                        }

                                        builder.Append($"%r{tempEndReg + 1} = getelementptr");
                                        builder.Append(" ");
                                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, j);
                                        builder.Append(", ");
                                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, j);
                                        builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg}");
                                        builder.AppendLine();

                                        arrayName = $"%r{tempEndReg + 1}";
                                        startReg = (int)tempEndReg + 2;
                                    }

                                    builder.Append($"%r{startReg} = getelementptr");
                                    builder.Append(" ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, elementAccessExpression.Arguments.Count);
                                    builder.Append(", ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, elementAccessExpression.Arguments.Count);
                                    builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} 0");
                                    builder.AppendLine();

                                    arrayName = $"%r{startReg}";
                                    startReg++;
                                }
                                else
                                {
                                    var argument1 = elementAccessExpression.Arguments[0];

                                    RealizeExpressionExcludeAssignment(argument1.Argument.Expression, scope, false, startReg, out int? tempEndReg, out _);

                                    if (tempEndReg is null)
                                    {
                                        throw new SemanticException();
                                    }

                                    builder.Append($"%r{tempEndReg + 1} = {_directives[SyntaxKind.IdentifierName]} {GenerateParameterArrayType(ranks, scope, baseType, 0)}, {GenerateParameterArrayType(ranks, scope, baseType, 0)} * {arrayName}");
                                    builder.AppendLine();

                                    builder.Append($"%r{tempEndReg + 2} = getelementptr");
                                    builder.Append(" ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, 1);
                                    builder.Append(", ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, 1);
                                    builder.Append($"* %r{tempEndReg + 1}, {_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg}");
                                    builder.AppendLine();

                                    arrayName = $"%r{tempEndReg + 2}";
                                    startReg = (int)tempEndReg + 3;

                                    if (elementAccessExpression.Arguments.Count > 1)
                                    {
                                        for (int j = 1; j < elementAccessExpression.Arguments.Count; j++)
                                        {
                                            argument1 = elementAccessExpression.Arguments[j];

                                            RealizeExpressionExcludeAssignment(argument1.Argument.Expression, scope, false, startReg, out int? tempEndReg1, out _);

                                            if (tempEndReg1 is null)
                                            {
                                                throw new SemanticException();
                                            }

                                            builder.Append($"%r{tempEndReg1 + 1} = getelementptr");
                                            builder.Append(" ");
                                            RealizeLocalDeclarationArrayType(ranks, scope, baseType, j);
                                            builder.Append(", ");
                                            RealizeLocalDeclarationArrayType(ranks, scope, baseType, j);
                                            builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg1}");
                                            builder.AppendLine();

                                            arrayName = $"%r{tempEndReg1 + 1}";
                                            startReg = (int)tempEndReg1 + 2;
                                        }

                                        builder.Append($"%r{startReg} = getelementptr");
                                        builder.Append(" ");
                                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, elementAccessExpression.Arguments.Count);
                                        builder.Append(", ");
                                        RealizeLocalDeclarationArrayType(ranks, scope, baseType, elementAccessExpression.Arguments.Count);
                                        builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} 0");
                                        builder.AppendLine();

                                        arrayName = $"%r{startReg}";
                                        startReg++;
                                    }
                                }

                                args[i] = $"{GenerateParameterArrayType(ranks, scope, baseType, elementAccessExpression.Arguments.Count)} {arrayName}";
                            }
                            else
                            {
                                throw new SemanticException();
                            }
                        }
                        else
                        {
                            var identifierName = argument.Expression as IdentifierNameSyntax;

                            if (scope.TryLookup(($"{identifierName.Identifier.Value}", VARIABLE), out var value))
                            {
                                var baseType = (value.Node.Kind is SyntaxKind.LocalDeclarationStatement ?
                                        (value.Node as LocalDeclarationStatementSyntax).Declaration.Type :
                                        (value.Node.Kind is SyntaxKind.FieldDeclaration ?
                                        (value.Node as FieldDeclarationSyntax).Declaration.Type :
                                        (value.Node as ParameterSyntax).Type));
                                int[] ranks = value.Properties[RANKS] as int[];

                                if (parameter.RankSpecifiers.Count != ranks.Length)
                                {
                                    throw new SemanticException();
                                }

                                string arrayName = value.Properties[UNIT_ACTUAL_NAME] as string;

                                if (value.Node.Kind is SyntaxKind.LocalDeclarationStatement or SyntaxKind.FieldDeclaration)
                                {
                                    builder.Append($"%r{startReg} = getelementptr");
                                    builder.Append(" ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, 0);
                                    builder.Append(", ");
                                    RealizeLocalDeclarationArrayType(ranks, scope, baseType, 0);
                                    builder.Append($"* {arrayName}, {_predefinedTypes[SyntaxKind.IntKeyword]} 0, {_predefinedTypes[SyntaxKind.IntKeyword]} 0");
                                    builder.AppendLine();

                                    arrayName = $"%r{startReg}";
                                    startReg++;
                                }
                                else
                                {
                                    builder.Append($"%r{startReg} = {_directives[SyntaxKind.IdentifierName]} {GenerateParameterArrayType(ranks, scope, baseType, 0)}, {GenerateParameterArrayType(ranks, scope, baseType, 0)} * {arrayName}");
                                    builder.AppendLine();

                                    arrayName = $"%r{startReg}";
                                    startReg++;
                                }

                                args[i] = $"{GenerateParameterArrayType(ranks, scope, baseType, 0)} {arrayName}";
                            }
                            else
                            {
                                throw new SemanticException();
                            }
                        }
                    }
                    else
                    {
                        RealizeExpressionExcludeAssignment(argument.Expression, scope, false, startReg, out int? tempEndReg, out _);

                        if (tempEndReg is null)
                        {
                            throw new SemanticException();
                        }

                        args[i] = $"{_predefinedTypes[SyntaxKind.IntKeyword]} %r{tempEndReg}";

                        startReg = (int)tempEndReg + 1;
                    }
                }

                endReg = lastReg = startReg - 1;

                arguments = args;
            }

            string GenerateParameterArrayType(int[] ranks, MemberScope scope, TypeSyntax baseType, int index)
            {
                return $"{GenerateParameterArrayTypeCore(ranks, scope, baseType, index + 1)}*";
            }

            string GenerateParameterArrayTypeCore(int[] ranks, MemberScope scope, TypeSyntax baseType, int index)
            {
                if (index >= ranks.Length)
                {
                    return GenerateType(baseType, scope);
                }

                int rank = ranks[index];

                return $"[{rank} x {GenerateParameterArrayTypeCore(ranks, scope, baseType, index + 1)}]";
            }

            string GenerateType(TypeSyntax type, MemberScope scope)
            {
                switch (type.Kind)
                {
                    case SyntaxKind.PredefinedType:
                        if (_predefinedTypes.TryGetValue((type as PredefinedTypeSyntax).Keyword.Kind, out var typeCode))
                        {
                            return typeCode;
                        }

                        break;
                    default:
                        break;
                }

                return string.Empty;
            }

            int CalculateExpressionExcludeAssignment(ExpressionSyntax expression, MemberScope scope)
            {
                switch (expression.Kind)
                {
                    case SyntaxKind.LogicalNotExpression:
                        throw new SemanticException();

                    case SyntaxKind.UnaryPlusExpression:
                    case SyntaxKind.UnaryMinusExpression:
                        {
                            var prefixUnaryExpression = expression as PrefixUnaryExpressionSyntax;

                            int res = CalculateExpressionExcludeAssignment(prefixUnaryExpression.Operand, scope);

                            return expression.Kind is SyntaxKind.UnaryMinusExpression ? -res : res;
                        }

                    case SyntaxKind.NumericLiteralExpression:
                        {
                            var literalExpression = expression as LiteralExpressionSyntax;

                            return (int)literalExpression.Token.Value;
                        }

                    case SyntaxKind.ParenthesizedExpression:
                        {
                            var parenthesizedExpression = expression as ParenthesizedExpressionSyntax;

                            int res = CalculateExpressionExcludeAssignment(parenthesizedExpression.Expression, scope);

                            return res;
                        }

                    case SyntaxKind.InvocationExpression:
                        throw new SemanticException();

                    case SyntaxKind.IdentifierName:
                        {
                            var identifierName = expression as IdentifierNameSyntax;

                            if (scope.TryLookup(($"{identifierName.Identifier.Value}", VARIABLE), out var value))
                            {
                                if (!HasConst(value.Node))
                                {
                                    throw new SemanticException();
                                }

                                int res = CalculateExpressionExcludeAssignment(GetConstantDeclarationExpression(value.Node, $"{identifierName.Identifier.Value}"), scope);

                                return res;
                            }
                            else
                            {
                                throw new SemanticException();
                            }
                        }

                    case SyntaxKind.ElementAccessExpression:
                        throw new SemanticException();

                    case SyntaxKind.AddExpression:
                    case SyntaxKind.SubtractExpression:
                    case SyntaxKind.MultiplyExpression:
                    case SyntaxKind.DivideExpression:
                    case SyntaxKind.ModuloExpression:
                    case SyntaxKind.LogicalOrExpression:
                    case SyntaxKind.LogicalAndExpression:
                    case SyntaxKind.EqualsExpression:
                    case SyntaxKind.NotEqualsExpression:
                    case SyntaxKind.LessThanExpression:
                    case SyntaxKind.LessThanOrEqualExpression:
                    case SyntaxKind.GreaterThanExpression:
                    case SyntaxKind.GreaterThanOrEqualExpression:
                        {
                            var binaryExpression = expression as BinaryExpressionSyntax;

                            int leftRes = CalculateExpressionExcludeAssignment(binaryExpression.Left, scope);

                            int rightRes = CalculateExpressionExcludeAssignment(binaryExpression.Right, scope);

                            return expression.Kind switch
                            {
                                SyntaxKind.ModuloExpression => leftRes % rightRes,
                                SyntaxKind.AddExpression => leftRes + rightRes,
                                SyntaxKind.SubtractExpression => leftRes - rightRes,
                                SyntaxKind.MultiplyExpression => leftRes * rightRes,
                                SyntaxKind.DivideExpression => leftRes / rightRes,
                                _ => throw new SemanticException()

                            };
                        }

                    default:
                        throw new SemanticException();
                }
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

            bool HasConst(SyntaxNode node)
            {
                return (node.Kind is SyntaxKind.LocalDeclarationStatement or SyntaxKind.FieldDeclaration ?
                        ContainsModifierKind((node.Kind is SyntaxKind.LocalDeclarationStatement ? (node as LocalDeclarationStatementSyntax).Modifiers : (node as FieldDeclarationSyntax).Modifiers), SyntaxKind.ConstKeyword) :
                        false);
            }

            ExpressionSyntax GetConstantDeclarationExpression(SyntaxNode node, string identifierName)
            {
                var variables = (node.Kind switch
                {
                    SyntaxKind.LocalDeclarationStatement => (node as LocalDeclarationStatementSyntax).Declaration,
                    SyntaxKind.FieldDeclaration => (node as FieldDeclarationSyntax).Declaration,
                    _ => throw new SemanticException()
                }).Variables;

                foreach (var variable in variables)
                {
                    if (identifierName.Equals(variable.Identifier.Value))
                    {
                        return variable.Initializer.Value;
                    }
                }

                return null;
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
            { SyntaxKind.ElementAccessExpression, "load" },
            { SyntaxKind.ReturnKeyword, "ret" },
            { SyntaxKind.VariableDeclarator, "alloca" },

            { SyntaxKind.LogicalOrExpression, "or" },
            { SyntaxKind.LogicalAndExpression, "and" },
            { SyntaxKind.LogicalNotExpression, "icmp eq" },
            { SyntaxKind.EqualsExpression, "icmp eq" },
            { SyntaxKind.NotEqualsExpression, "icmp ne" },
            { SyntaxKind.LessThanExpression, "icmp slt" },
            { SyntaxKind.LessThanOrEqualExpression, "icmp sle" },
            { SyntaxKind.GreaterThanExpression, "icmp sgt" },
            { SyntaxKind.GreaterThanOrEqualExpression, "icmp sge" }
        };

        private const string MAIN = "main";

        private const string METHOD = "method";
        private const string VARIABLE = "variable";

        private const string RANKS = "ranks";
        private const string UNIT_ACTUAL_NAME = "unit_actual_name";
    }
}
