using System;

namespace Crossdox.DocTypes
{
	[Flags]
	public enum NameFlags
	{
		Private = (1 << 0),
		Protected = (1 << 1),
		Internal = (1 << 2),
		Public = (1 << 3),

		AllVisibilities = Public | Internal | Protected | Private,

		Static = (1 << 4),
		Sealed = (1 << 5),
		Abstract = (1 << 6),
		Virtual = (1 << 7),
		New = (1 << 8),

		Const = (1 << 9),
		ReadOnly = (1 << 10),

		Method = (1 << 16),
		ClassConstructor = (1 << 17),
		Constructor = (1 << 18),
		SpecialName = (1 << 19),
		ExplicitInterfaceImplementation = (1 << 20),
	}
}
