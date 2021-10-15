using System;
using System.Collections.Generic;

namespace BUAA.CodeAnalysis.MiniSysY.Internals
{
    internal class SyntaxTokenListViewer
    {
        private int _position;

        private readonly IReadOnlyList<SyntaxToken> _syntaxTokens;

        public SyntaxTokenListViewer(IReadOnlyList<SyntaxToken> syntaxTokens)
        {
            _syntaxTokens = syntaxTokens ?? throw new ArgumentNullException(nameof(syntaxTokens));
            _position = 0;
        }

        public bool IsIndexAtEnd => _position >= _syntaxTokens.Count;

        public int Position => _position;

        public void AdvanceToken()
        {
            _position++;
        }

        public void AdvanceToken(int n)
        {
            _position += n;
        }

        public SyntaxToken NextToken()
        {
            var token = PeekToken();

            if (token != SyntaxToken.Empty)
            {
                AdvanceToken();
            }

            return token;
        }

        public SyntaxToken PeekToken()
        {
            if (_position >= _syntaxTokens.Count)
            {
                return SyntaxToken.Empty;
            }

            return _syntaxTokens[_position];
        }

        public SyntaxToken PeekToken(int delta)
        {
            int peekIndex = _position + delta;

            if (peekIndex >= _syntaxTokens.Count)
            {
                return SyntaxToken.Empty;
            }

            return _syntaxTokens[peekIndex];
        }

        public void Reset(int position)
        {
            _position = position >= 0 ? position : 0;
        }
    }
}
