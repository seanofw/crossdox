namespace Crossdox.Xml
{
	internal enum NameTokenKind
	{
		Error = -2,
		EOI = -1,
		None = 0,

		LeftParenthesis,
		RightParenthesis,
		LeftBracket,
		RightBracket,
		LeftCurlyBrace,
		RightCurlyBrace,

		At,
		Comma,
		Colon,
		Period,
		Backtick,
		DoubleBacktick,
		Sharp,
		DoubleSharp,

		Name,
		Number,
	}
}
