using System.Collections.Generic;

namespace Crossdox.Templating
{
	internal class TemplateLexer
	{
		public static List<Token> Tokenize(string text)
		{
			List<Token> tokens = new List<Token>();

			int end = text.Length;
			int src = 0;
			int line = 1;
			int lineStart = 0;

			int start = src;
			bool lineHasContent = false;

			// Skip an initial BOM, if one exists.
			if (text.Length > 3 && text[0] == 0xEF && text[1] == 0xBB && text[2] == 0xBF)
				start = (src += 3);
			if (text.Length > 1 && (text[0] == 0xFEFF || text[0] == 0xFFFE))
				start = (src += 1);

			while (src < end)
			{
				switch (text[src++])
				{
					case '\x00': case '\x01': case '\x02': case '\x03':
					case '\x04': case '\x05': case '\x06': case '\x07':
					case '\x08': case '\x09':              case '\x0B':
					case '\x0C':              case '\x0E': case '\x0F':
					case '\x10': case '\x11': case '\x12': case '\x13':
					case '\x14': case '\x15': case '\x16': case '\x17':
					case '\x18': case '\x19': case '\x1A': case '\x1B':
					case '\x1C': case '\x1D': case '\x1E': case '\x1F':
						break;

					case '\x0A':
						if (src - 1 > start)
							tokens.Add(new Token(TokenKind.PlainText, text, line, lineStart, start, src - 1 - start));
						start = src - 1;
						if (src < end && text[src] == '\x0D')
							src++;
						tokens.Add(new Token(TokenKind.Newline, text, line, lineStart, start, src - start));
						start = src;
						line++;
						lineStart = src;
						lineHasContent = false;
						break;

					case '\x0D':
						if (src - 1 > start)
							tokens.Add(new Token(TokenKind.PlainText, text, line, lineStart, start, src - 1 - start));
						start = src - 1;
						if (src < end && text[src] == '\x0A')
							src++;
						tokens.Add(new Token(TokenKind.Newline, text, line, lineStart, start, src - start));
						start = src;
						line++;
						lineStart = src;
						lineHasContent = false;
						break;

					case '{':
						if (src - 1 > start)
							tokens.Add(new Token(TokenKind.PlainText, text, line, lineStart, start, src - 1 - start));
						if (src < end && text[src] == '{')
						{
							tokens.Add(new Token(TokenKind.PlainText, text, line, lineStart, start, src - start));
							src++;
							start = src;
						}
						else
						{
							for (start = src; src < end && text[src] != '}' && text[src] != '\x0A' && text[src] != '\x0D'; src++) ;
							if (src >= end || text[src] != '}' || src <= start)
								tokens.Add(new Token(TokenKind.Error, text, line, lineStart, start, src - start));
							else
							{
								tokens.Add(new Token(TokenKind.Expression, text, line, lineStart, start, src - start));
								src++;
							}
							start = src;
						}
						lineHasContent = true;
						break;

					case '%':
						if (lineHasContent)
							break;	// If we already had content before this, it's not a statement.

						for (start = src; src < end && text[src] != '\x0A' && text[src] != '\x0D'; src++) ;
						if (src <= start)
							break;  // Ignore blank-ish lines.

						tokens.Add(new Token(TokenKind.Statement, text, line, lineStart, start, src - start));
						start = src;
						break;

					default:
						lineHasContent = true;
						break;
				}
			}

			if (src > start)
				tokens.Add(new Token(TokenKind.PlainText, text, line, lineStart, start, src - start));

			return tokens;
		}
	}
}
