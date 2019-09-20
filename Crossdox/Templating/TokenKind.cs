namespace Crossdox.Templating
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
