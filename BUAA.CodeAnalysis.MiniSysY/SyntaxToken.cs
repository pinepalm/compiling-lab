using System;

namespace BUAA.CodeAnalysis.MiniSysY
{
    public readonly struct SyntaxToken : IEquatable<SyntaxToken>
    {
        public static readonly SyntaxToken Empty = new()
        {
            Kind = SyntaxKind.None,
            Text = null,
            Value = null
        };

        public SyntaxKind Kind { get; init; }

        public string Text { get; init; }

        public object Value { get; init; }

        public bool Equals(SyntaxToken other)
        {
            return Kind == other.Kind && Text == other.Text && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is SyntaxToken token && Equals(token);
        }

        public override int GetHashCode()
        {
            return (Kind, Text, Value).GetHashCode();
        }

        public static bool operator ==(SyntaxToken lhs, SyntaxToken rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(SyntaxToken lhs, SyntaxToken rhs)
        {
            return !(lhs == rhs);
        }
    }
}
