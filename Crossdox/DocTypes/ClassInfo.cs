using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Crossdox.DocTypes
{
	public class ClassInfo : IEquatable<ClassInfo>
	{
		public string Name { get; }
		public IList<string> TypeParameters { get; }
		public NameFlags Flags { get; }

		private readonly string _stringified;

		public ClassInfo(string name, IEnumerable<string> typeParameters, NameFlags flags)
		{
			Name = name ?? string.Empty;
			TypeParameters = new ReadOnlyCollection<string>(typeParameters != null ? typeParameters.ToArray() : new string[0]);
			Flags = flags;

			_stringified = Stringify();
		}

		public ClassInfo ReplaceTypeParameterNames(Func<string, string> replacer)
			=> new ClassInfo(Name, TypeParameters.Select(t => replacer(t)), Flags);

		public string ClrName
			=> TypeParameters.Count > 0 ? Name + "`" + TypeParameters.Count : Name;

		public override string ToString()
			=> _stringified;

		private string Stringify()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(Name);
			NameInfo.AppendTypeParameters(stringBuilder, TypeParameters);
			return stringBuilder.ToString();
		}

		public override bool Equals(object obj)
			=> Equals(obj as ClassInfo);
		public bool Equals(ClassInfo other)
			=> !ReferenceEquals(other, null)
				&& _stringified == other._stringified;
		public override int GetHashCode()
			=> _stringified.GetHashCode();

		public static bool operator ==(ClassInfo a, ClassInfo b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(ClassInfo a, ClassInfo b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);
	}

}