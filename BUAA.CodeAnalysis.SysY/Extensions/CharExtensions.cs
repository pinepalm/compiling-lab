namespace BUAA.CodeAnalysis.SysY
{
    public static class CharExtensions
    {
        public static bool IsLetter(this char @char)
        {
            return char.IsLetter(@char);
        }

        public static bool IsUnderline(this char @char)
        {
            return @char is '_';
        }

        public static bool IsEqualSign(this char @char)
        {
            return @char is '=';
        }

        public static bool IsDigit(this char @char)
        {
            return char.IsDigit(@char);
        }

        public static bool IsLetterOrUnderline(this char @char)
        {
            return @char.IsLetter() || @char.IsUnderline();
        }

        public static bool IsLetterOrDigit(this char @char)
        {
            return char.IsLetterOrDigit(@char);
        }

        public static bool IsLetterOrUnderlineOrDigit(this char @char)
        {
            return @char.IsLetterOrUnderline() || @char.IsDigit();
        }

        public static bool IsWhiteSpace(this char @char)
        {
            return char.IsWhiteSpace(@char);
        }
    }
}
