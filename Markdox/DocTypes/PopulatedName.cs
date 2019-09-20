using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Markdox.DocTypes
{
	public class PopulatedName : IEquatable<PopulatedName>
	{
		public string Name { get; }
		public IList<PopulatedType> TypeParameters { get; }

		private readonly string _stringified;

		public PopulatedName(string name, IEnumerable<PopulatedType> typeParameters = null)
		{
			Name = name;
			TypeParameters = new ReadOnlyCollection<PopulatedType>(typeParameters?.ToArray() ?? new PopulatedType[0]);
			_stringified = Stringify();
		}

		public PopulatedName ReplaceTypeParameterNames(Func<string, string> replacer)
			=> new PopulatedName(replacer(Name),
				TypeParameters.Select(t => t.ReplaceTypeParameterNames(replacer)));

		private string Stringify()
		{
			if (!TypeParameters.Any())
				return Name;

			StringBuilder stringBuilder = new StringBuilder();

			stringBuilder.Append(Name);
			stringBuilder.Append('<');
			bool isFirst = true;
			foreach (PopulatedType populatedType in TypeParameters)
			{
				if (!isFirst)
					stringBuilder.Append(", ");
				stringBuilder.Append(populatedType);
				isFirst = false;
			}
			stringBuilder.Append('>');

			return stringBuilder.ToString();
		}

		public override bool Equals(object obj)
			=> Equals(obj as PopulatedName);
		public bool Equals(PopulatedName other)
			=> !ReferenceEquals(other, null)
				&& Name == other.Name
				&& TypeParameters.SequenceEqual(other.TypeParameters);
		public override int GetHashCode()
			=> _stringified.GetHashCode();
		public override string ToString()
			=> _stringified;
	}
}
