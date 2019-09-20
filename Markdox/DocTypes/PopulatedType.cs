using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Markdox.DocTypes
{
	/// <summary>
	/// A PopulatedType represents a complete fully-qualified type-name, with all
	/// generic type parameters filled in with real types (doesn't matter what kind
	/// of types, just that they're all filled in).
	/// </summary>
	public class PopulatedType : IEquatable<PopulatedType>
	{
		public IList<PopulatedName> Names { get; }

		public IList<ArrayInfo> Arrays { get; }

		private readonly string _stringified;

		public PopulatedType(params PopulatedName[] names)
			: this((IEnumerable<PopulatedName>)names)
		{
		}

		public PopulatedType(IEnumerable<PopulatedName> names, IEnumerable<ArrayInfo> arrays = null)
		{
			Names = new ReadOnlyCollection<PopulatedName>(names?.ToArray() ?? new PopulatedName[0]);
			Arrays = new ReadOnlyCollection<ArrayInfo>(arrays?.ToArray() ?? new ArrayInfo[0]);

			_stringified = string.Join('.', Names) + string.Join("", Arrays);
		}

		public PopulatedType WithNames(IEnumerable<PopulatedName> names)
			=> new PopulatedType(names, Arrays);
		public PopulatedType WithArrays(IEnumerable<ArrayInfo> arrays)
			=> new PopulatedType(Names, arrays);

		public PopulatedType ReplaceTypeParameterNames(Func<string, string> replacer)
			=> new PopulatedType(Names.Select(n => n.ReplaceTypeParameterNames(replacer)), Arrays);

		public override bool Equals(object obj)
			=> Equals(obj as PopulatedType);
		public bool Equals(PopulatedType other)
			=> !ReferenceEquals(other, null)
				&& Names.SequenceEqual(other.Names);
		public override int GetHashCode()
			=> _stringified.GetHashCode();
		public override string ToString()
			=> _stringified;
	}
}
