using System;

namespace BUAA.CodeAnalysis.MiniSysY.Internals
{
    internal class TextViewer
    {
        public const char InvalidCharacter = char.MaxValue;

        private int _position;

        private readonly string _text;

        public TextViewer(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _position = 0;
        }

        public bool IsIndexAtEnd => _position >= _text.Length;

        public int Position => _position;

        public void AdvanceChar()
        {
            _position++;
        }

        public void AdvanceChar(int n)
        {
            _position += n;
        }

        public char NextChar()
        {
            char c = PeekChar();

            if (c is not InvalidCharacter)
            {
                AdvanceChar();
            }

            return c;
        }

        public char PeekChar()
        {
            if (_position >= _text.Length)
            {
                return InvalidCharacter;
            }

            return _text[_position];
        }

        public char PeekChar(int delta)
        {
            int peekIndex = _position + delta;

            if (peekIndex >= _text.Length)
            {
                return InvalidCharacter;
            }

            return _text[peekIndex];
        }

        public void Reset(int position)
        {
            _position = position >= 0 ? position : 0;
        }
    }
}
