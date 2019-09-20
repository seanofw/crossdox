namespace Markdox.Xml
{
	internal class NameLexer
	{
		private readonly string _text;
		private int _src;
		private readonly int _end;
		private bool _unget;

		public NameToken Token;

		public NameLexer(string text)
		{
			_text = text;
			_end = text.Length;
			Token = new NameToken(NameTokenKind.None, 0, 0, _text);
		}

		public void Unget()
		{
			_unget = true;
		}

		public NameTokenKind Next()
		{
			if (_unget)
			{
				_unget = false;
				return Token.Kind;
			}

		retry:
			if (_src >= _end)
				return (Token = new NameToken(NameTokenKind.EOI, _src, 0, _text)).Kind;

			int start = _src;
			switch (_text[_src++])
			{
				case '\x00': case '\x01': case '\x02': case '\x03':
				case '\x04': case '\x05': case '\x06': case '\x07':
				case '\x08': case '\x09': case '\x0A': case '\x0B':
				case '\x0C': case '\x0D': case '\x0E': case '\x0F':
				case '\x10': case '\x11': case '\x12': case '\x13':
				case '\x14': case '\x15': case '\x16': case '\x17':
				case '\x18': case '\x19': case '\x1A': case '\x1B':
				case '\x1C': case '\x1D': case '\x1E': case '\x1F':
					goto retry;

				case '(':
					return (Token = new NameToken(NameTokenKind.LeftParenthesis, start, _src - start, _text)).Kind;
				case ')':
					return (Token = new NameToken(NameTokenKind.RightParenthesis, start, _src - start, _text)).Kind;
				case '[':
					return (Token = new NameToken(NameTokenKind.LeftBracket, start, _src - start, _text)).Kind;
				case ']':
					return (Token = new NameToken(NameTokenKind.RightBracket, start, _src - start, _text)).Kind;
				case '{':
					return (Token = new NameToken(NameTokenKind.LeftCurlyBrace, start, _src - start, _text)).Kind;
				case '}':
					return (Token = new NameToken(NameTokenKind.RightCurlyBrace, start, _src - start, _text)).Kind;

				case '@':
					return (Token = new NameToken(NameTokenKind.At, start, _src - start, _text)).Kind;
				case '.':
					return (Token = new NameToken(NameTokenKind.Period, start, _src - start, _text)).Kind;
				case ',':
					return (Token = new NameToken(NameTokenKind.Comma, start, _src - start, _text)).Kind;
				case ':':
					return (Token = new NameToken(NameTokenKind.Colon, start, _src - start, _text)).Kind;

				case '#':
					if (_src < _end && _text[_src] == '#')
					{
						_src++;
						return (Token = new NameToken(NameTokenKind.DoubleSharp, start, _src - start, _text)).Kind;
					}
					return (Token = new NameToken(NameTokenKind.Sharp, start, _src - start, _text)).Kind;

				case '`':
					if (_src < _end && _text[_src] == '`')
					{
						_src++;
						return (Token = new NameToken(NameTokenKind.DoubleBacktick, start, _src - start, _text)).Kind;
					}
					return (Token = new NameToken(NameTokenKind.Backtick, start, _src - start, _text)).Kind;

				case '0': case '1': case '2': case '3': case '4':
				case '5': case '6': case '7': case '8': case '9':
					{
						char ch;
						while (_src < _end && (ch = _text[_src]) >= '0' && ch <= '9')
						{
							_src++;
						}
						return (Token = new NameToken(NameTokenKind.Number, start, _src - start, _text)).Kind;
					}

				case 'a': case 'b': case 'c': case 'd': case 'e': case 'f': case 'g': case 'h':
				case 'i': case 'j': case 'k': case 'l': case 'm': case 'n': case 'o': case 'p':
				case 'q': case 'r': case 's': case 't': case 'u': case 'v': case 'w': case 'x':
				case 'y': case 'z':
				case 'A': case 'B': case 'C': case 'D': case 'E': case 'F': case 'G': case 'H':
				case 'I': case 'J': case 'K': case 'L': case 'M': case 'N': case 'O': case 'P':
				case 'Q': case 'R': case 'S': case 'T': case 'U': case 'V': case 'W': case 'X':
				case 'Y': case 'Z':
				case '_':
				lexName:
					if (_src < _end)
					{
						switch (_text[_src])
						{
							case 'a': case 'b': case 'c': case 'd': case 'e': case 'f': case 'g': case 'h':
							case 'i': case 'j': case 'k': case 'l': case 'm': case 'n': case 'o': case 'p':
							case 'q': case 'r': case 's': case 't': case 'u': case 'v': case 'w': case 'x':
							case 'y': case 'z':
							case 'A': case 'B': case 'C': case 'D': case 'E': case 'F': case 'G': case 'H':
							case 'I': case 'J': case 'K': case 'L': case 'M': case 'N': case 'O': case 'P':
							case 'Q': case 'R': case 'S': case 'T': case 'U': case 'V': case 'W': case 'X':
							case 'Y': case 'Z':
							case '_':
							case '0': case '1': case '2': case '3': case '4':
							case '5': case '6': case '7': case '8': case '9':
								_src++;
								goto lexName;
							default:
								if (char.IsLetter(_text[_src]))
								{
									_src++;
									goto lexName;
								}
								break;
						}
					}
					return (Token = new NameToken(NameTokenKind.Name, start, _src - start, _text)).Kind;

				default:
					if (char.IsLetter(_text[_src - 1]))
						goto lexName;
					return (Token = new NameToken(NameTokenKind.Error, start, _src - start, _text)).Kind;
			}
		}
	}
}
