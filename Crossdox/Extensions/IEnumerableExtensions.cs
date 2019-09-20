using System.Collections.Generic;
using System.Collections.Immutable;

namespace Crossdox.Extensions
{
	public static class IEnumerableExtensions
	{
		public static ImmutableList<T> MakeImmutableList<T>(this IEnumerable<T> items)
			=> (items is ImmutableList<T> list) ? list
				: items != null ? ImmutableList<T>.Empty.AddRange(items)
				: ImmutableList<T>.Empty;
	}
}
