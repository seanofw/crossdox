using System;

namespace Markdox
{
	[Flags]
	public enum IncludeKind
	{
		Public = (1 << 0),
		Internal = (1 << 1),
		Protected = (1 << 2),
		Private = (1 << 3),

		All = Public | Internal | Protected | Private,
		None = 0,
	}
}