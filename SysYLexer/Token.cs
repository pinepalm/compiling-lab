namespace SysYLexer
{
    public class Token
    {
        public TokenType Type { get; set; }

        public string Text { get; set; }

        public override string ToString()
        {
            var type = Type;
            var value = Text;

            return type switch
            {
                TokenType.Ident => $"{type}({value})",
                TokenType.Number => $"{type}({value})",
                _ => $"{type}"
            };
        }
    }
}
