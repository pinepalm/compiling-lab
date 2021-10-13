namespace BUAA.CodeAnalysis.MiniSysY
{
    public class MiniSysYToken
    {
        public MiniSysYTokenType Type { get; set; }

        public string Text { get; set; }

        public override string ToString()
        {
            var type = Type;
            var value = Text;

            return type switch
            {
                MiniSysYTokenType.Ident => $"{type}({value})",
                MiniSysYTokenType.Number => $"{type}({value})",
                _ => $"{type}"
            };
        }
    }
}
