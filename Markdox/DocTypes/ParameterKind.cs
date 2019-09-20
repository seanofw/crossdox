using System;

namespace Markdox.DocTypes
{
	[Flags]
	public enum ParameterKind
	{
		In = (1 << 0),
		Out = (1 << 1),
		Ref = (1 << 2),
		Optional = (1 << 3),
		HasDefaultValue = (1 << 4),
	}
}