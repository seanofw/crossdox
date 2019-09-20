namespace Markdox.Templating
{
	internal enum TokenKind
	{
		Error = -1,
		None,

		Expression,
		Statement,
		PlainText,
		Newline,
	}
}
