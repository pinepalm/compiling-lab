namespace BUAA.CodeAnalysis.SysY
{
    public class SysYToken
    {
        public SysYTokenType Type { get; set; }

        public string Text { get; set; }

        public override string ToString()
        {
            var type = Type;
            var value = Text;

            return type switch
            {
                SysYTokenType.Ident => $"{type}({value})",
                SysYTokenType.Number => $"{type}({value})",
                _ => $"{type}"
            };
        }
    }
}
