namespace Markdox.Templating
{
	internal class Token
	{
		public TokenKind Kind { get; }

		public int Line { get; }        // First line this token was found on.
		public int LineStart { get; }   // Start offset in file of the start of this line.
		public int Start { get; }       // Start offset in file of the start of this token.
		public int Length { get; }      // Length of this token.

		private string _text;

		public string Text => _text.Substring(Start, Length);

		internal Token(TokenKind kind, string text, int line, int lineStart, int start, int length)
		{
			Kind = kind;
			_text = text;
			Line = line;
			LineStart = lineStart;
			Start = start;
			Length = length;
		}

		public override string ToString()
			=> $"{Kind}: {Text}";
	}
}
