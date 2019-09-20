using System;

namespace Markdox.Xml
{
	internal struct NameToken : IEquatable<NameToken>
	{
		public NameTokenKind Kind { get; }
		public int Start { get; }
		public int Length { get; }

		private readonly string _fullText;

		public NameToken(NameTokenKind kind, int start, int length, string fullText)
		{
			Kind = kind;
			Start = start;
			Length = length;
			_fullText = fullText;
		}

		public string Text => _fullText.Substring(Start, Length);

		public override bool Equals(object obj)
			=> obj is NameToken token && Equals(token);

		public bool Equals(NameToken token)
			=> Kind == token.Kind &&
				Start == token.Start &&
				Length == token.Length &&
				_fullText == token._fullText;

		public override int GetHashCode()
			=> HashCode.Combine(Kind, Start, Length, _fullText);

		public static bool operator ==(NameToken a, NameToken b)
			=> a.Equals(b);
		public static bool operator !=(NameToken a, NameToken b)
			=> !a.Equals(b);

		public override string ToString()
			=> $"{Kind}: (at {Start} +{Length}) \"{Text}\"";
	}
}
