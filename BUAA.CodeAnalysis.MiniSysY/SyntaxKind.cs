namespace BUAA.CodeAnalysis.MiniSysY
{
    public enum SyntaxKind
    {
        None,
        IdentifierToken,
        NumericLiteralToken,
        IntKeyword,
        ReturnKeyword,
        VoidKeyword,
        OpenBraceToken,
        CloseBraceToken,
        OpenParenToken,
        CloseParenToken,
        SemicolonToken,
        SingleLineCommentTrivia,
        MultiLineCommentTrivia,
        CompilationUnit,
        Block,
        ArrayType,
        PredefinedType,
        Parameter,
        ParameterList,
        ReturnStatement,
        MethodDeclaration,
        // this is a temp kind
        Expression
    }
}
