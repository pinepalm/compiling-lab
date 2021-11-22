using System;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public readonly struct SyntaxTrivia : IEquatable<SyntaxTrivia>
    {
        public static readonly SyntaxTrivia Empty = new()
        {
            Kind = SyntaxKind.None,
        };

        public SyntaxKind Kind { get; init; }

        public bool Equals(SyntaxTrivia other)
        {
            return Kind == other.Kind;
        }

        public override bool Equals(object obj)
        {
            return obj is SyntaxTrivia trivia && Equals(trivia);
        }

        public override int GetHashCode()
        {
            return (Kind).GetHashCode();
        }

        public static bool operator ==(SyntaxTrivia lhs, SyntaxTrivia rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(SyntaxTrivia lhs, SyntaxTrivia rhs)
        {
            return !(lhs == rhs);
        }
    }
}
