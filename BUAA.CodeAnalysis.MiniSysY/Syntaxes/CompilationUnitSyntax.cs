using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public class CompilationUnitSyntax : SyntaxNode
    {
        internal CompilationUnitSyntax()
            : base(SyntaxKind.CompilationUnit)
        {

        }

        public IReadOnlyList<MemberDeclarationSyntax> Members { get; init; }

        public CompilationUnitSyntax WithRuntimeMethods()
        {
            var members = Members;
            var runtimeMethods = new List<MemberDeclarationSyntax>();

            runtimeMethods.Add(new MethodDeclarationSyntax()
            {
                Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                ReturnType = new PredefinedTypeSyntax()
                {
                    Keyword = new SyntaxToken()
                    {
                        Kind = SyntaxKind.IntKeyword,
                        Text = "int",
                        Value = "int"
                    }
                },
                Identifier = new SyntaxToken()
                {
                    Kind = SyntaxKind.IdentifierToken,
                    Text = "getint",
                    Value = "getint",
                },
                ParameterList = new ParameterListSyntax()
                {
                    OpenParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.OpenParenToken,
                        Text = "(",
                        Value = null
                    },
                    Parameters = (new List<ParameterSyntax>()).AsReadOnly(),
                    CloseParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.CloseParenToken,
                        Text = ")",
                        Value = null
                    }
                },
                Body = null,
                SemicolonToken = new SyntaxToken()
                {
                    Kind = SyntaxKind.SemicolonToken,
                    Text = ";",
                    Value = null
                }
            });

            runtimeMethods.Add(new MethodDeclarationSyntax()
            {
                Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                ReturnType = new PredefinedTypeSyntax()
                {
                    Keyword = new SyntaxToken()
                    {
                        Kind = SyntaxKind.IntKeyword,
                        Text = "int",
                        Value = "int"
                    }
                },
                Identifier = new SyntaxToken()
                {
                    Kind = SyntaxKind.IdentifierToken,
                    Text = "getch",
                    Value = "getch",
                },
                ParameterList = new ParameterListSyntax()
                {
                    OpenParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.OpenParenToken,
                        Text = "(",
                        Value = null
                    },
                    Parameters = (new List<ParameterSyntax>()).AsReadOnly(),
                    CloseParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.CloseParenToken,
                        Text = ")",
                        Value = null
                    }
                },
                Body = null,
                SemicolonToken = new SyntaxToken()
                {
                    Kind = SyntaxKind.SemicolonToken,
                    Text = ";",
                    Value = null
                }
            });

            runtimeMethods.Add(new MethodDeclarationSyntax()
            {
                Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                ReturnType = new PredefinedTypeSyntax()
                {
                    Keyword = new SyntaxToken()
                    {
                        Kind = SyntaxKind.VoidKeyword,
                        Text = "void",
                        Value = "void"
                    }
                },
                Identifier = new SyntaxToken()
                {
                    Kind = SyntaxKind.IdentifierToken,
                    Text = "putint",
                    Value = "putint",
                },
                ParameterList = new ParameterListSyntax()
                {
                    OpenParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.OpenParenToken,
                        Text = "(",
                        Value = null
                    },
                    Parameters = (new List<ParameterSyntax>()
                    {
                        new ParameterSyntax()
                        {
                            Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                            Type = new PredefinedTypeSyntax()
                            {
                                Keyword = new SyntaxToken()
                                {
                                    Kind = SyntaxKind.IntKeyword,
                                    Text = "int",
                                    Value = "int"
                                }
                            },
                            Identifier = null,
                            RankSpecifiers = null
                        }
                    }).AsReadOnly(),
                    CloseParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.CloseParenToken,
                        Text = ")",
                        Value = null
                    }
                },
                Body = null,
                SemicolonToken = new SyntaxToken()
                {
                    Kind = SyntaxKind.SemicolonToken,
                    Text = ";",
                    Value = null
                }
            });

            runtimeMethods.Add(new MethodDeclarationSyntax()
            {
                Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                ReturnType = new PredefinedTypeSyntax()
                {
                    Keyword = new SyntaxToken()
                    {
                        Kind = SyntaxKind.VoidKeyword,
                        Text = "void",
                        Value = "void"
                    }
                },
                Identifier = new SyntaxToken()
                {
                    Kind = SyntaxKind.IdentifierToken,
                    Text = "putch",
                    Value = "putch",
                },
                ParameterList = new ParameterListSyntax()
                {
                    OpenParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.OpenParenToken,
                        Text = "(",
                        Value = null
                    },
                    Parameters = (new List<ParameterSyntax>()
                    {
                        new ParameterSyntax()
                        {
                            Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                            Type = new PredefinedTypeSyntax()
                            {
                                Keyword = new SyntaxToken()
                                {
                                    Kind = SyntaxKind.IntKeyword,
                                    Text = "int",
                                    Value = "int"
                                }
                            },
                            Identifier = null,
                            RankSpecifiers = null
                        }
                    }).AsReadOnly(),
                    CloseParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.CloseParenToken,
                        Text = ")",
                        Value = null
                    }
                },
                Body = null,
                SemicolonToken = new SyntaxToken()
                {
                    Kind = SyntaxKind.SemicolonToken,
                    Text = ";",
                    Value = null
                }
            });

            runtimeMethods.Add(new MethodDeclarationSyntax()
            {
                Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                ReturnType = new PredefinedTypeSyntax()
                {
                    Keyword = new SyntaxToken()
                    {
                        Kind = SyntaxKind.IntKeyword,
                        Text = "int",
                        Value = "int"
                    }
                },
                Identifier = new SyntaxToken()
                {
                    Kind = SyntaxKind.IdentifierToken,
                    Text = "getarray",
                    Value = "getarray",
                },
                ParameterList = new ParameterListSyntax()
                {
                    OpenParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.OpenParenToken,
                        Text = "(",
                        Value = null
                    },
                    Parameters = (new List<ParameterSyntax>()
                    {
                        new ParameterSyntax()
                        {
                            Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                            Type = new PredefinedTypeSyntax()
                            {
                                Keyword = new SyntaxToken()
                                {
                                    Kind = SyntaxKind.IntKeyword,
                                    Text = "int",
                                    Value = "int"
                                }
                            },
                            Identifier = null,
                            RankSpecifiers = (new List<ArrayRankSpecifierSyntax>()
                            {
                                new ArrayRankSpecifierSyntax()
                                {
                                    OpenBracketToken = new SyntaxToken()
                                    {
                                        Kind = SyntaxKind.OpenBracketToken,
                                        Text = "[",
                                        Value = null
                                    },
                                    Size = null,
                                    CloseBracketToken = new SyntaxToken()
                                    {
                                        Kind = SyntaxKind.CloseBracketToken,
                                        Text = "]",
                                        Value = null
                                    }
                                }
                            }).AsReadOnly()
                        }
                    }).AsReadOnly(),
                    CloseParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.CloseParenToken,
                        Text = ")",
                        Value = null
                    }
                },
                Body = null,
                SemicolonToken = new SyntaxToken()
                {
                    Kind = SyntaxKind.SemicolonToken,
                    Text = ";",
                    Value = null
                }
            });

            runtimeMethods.Add(new MethodDeclarationSyntax()
            {
                Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                ReturnType = new PredefinedTypeSyntax()
                {
                    Keyword = new SyntaxToken()
                    {
                        Kind = SyntaxKind.VoidKeyword,
                        Text = "void",
                        Value = "void"
                    }
                },
                Identifier = new SyntaxToken()
                {
                    Kind = SyntaxKind.IdentifierToken,
                    Text = "putarray",
                    Value = "putarray",
                },
                ParameterList = new ParameterListSyntax()
                {
                    OpenParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.OpenParenToken,
                        Text = "(",
                        Value = null
                    },
                    Parameters = (new List<ParameterSyntax>()
                    {
                        new ParameterSyntax()
                        {
                            Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                            Type = new PredefinedTypeSyntax()
                            {
                                Keyword = new SyntaxToken()
                                {
                                    Kind = SyntaxKind.IntKeyword,
                                    Text = "int",
                                    Value = "int"
                                }
                            },
                            Identifier = null,
                            RankSpecifiers = null
                        },
                        new ParameterSyntax()
                        {
                            Modifiers = (new List<SyntaxToken>()).AsReadOnly(),
                            Type = new PredefinedTypeSyntax()
                            {
                                Keyword = new SyntaxToken()
                                {
                                    Kind = SyntaxKind.IntKeyword,
                                    Text = "int",
                                    Value = "int"
                                }
                            },
                            Identifier = null,
                            RankSpecifiers = (new List<ArrayRankSpecifierSyntax>()
                            {
                                new ArrayRankSpecifierSyntax()
                                {
                                    OpenBracketToken = new SyntaxToken()
                                    {
                                        Kind = SyntaxKind.OpenBracketToken,
                                        Text = "[",
                                        Value = null
                                    },
                                    Size = null,
                                    CloseBracketToken = new SyntaxToken()
                                    {
                                        Kind = SyntaxKind.CloseBracketToken,
                                        Text = "]",
                                        Value = null
                                    }
                                }
                            }).AsReadOnly()
                        }
                    }).AsReadOnly(),
                    CloseParenToken = new SyntaxToken()
                    {
                        Kind = SyntaxKind.CloseParenToken,
                        Text = ")",
                        Value = null
                    }
                },
                Body = null,
                SemicolonToken = new SyntaxToken()
                {
                    Kind = SyntaxKind.SemicolonToken,
                    Text = ";",
                    Value = null
                }
            });

            runtimeMethods.AddRange(members);

            var membersWithRuntimeMethods = runtimeMethods;

            return new CompilationUnitSyntax()
            {
                Members = membersWithRuntimeMethods.AsReadOnly()
            };
        }

        public SyntaxTree AsSyntaxTree()
        {
            return new SyntaxTree()
            {
                CompilationUnits = (new List<CompilationUnitSyntax>()
                {
                    this
                }).AsReadOnly()
            };
        }
    }
}
