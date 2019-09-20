using System;

namespace Crossdox.DocTypes
{
	public class TypeParameterDoc : IEquatable<TypeParameterDoc>
	{
		public string Name { get; }
		public string Description { get; }

		public TypeParameterDoc(string name, string description)
		{
			Name = name;
			Description = description;
		}

		public TypeParameterDoc WithName(string name)
			=> new TypeParameterDoc(name, Description);
		public TypeParameterDoc WithMeta(string description)
			=> new TypeParameterDoc(Name, description);

		public override bool Equals(object obj)
			=> Equals(obj as TypeParameterDoc);
		public bool Equals(TypeParameterDoc other)
			=> other != null
				&& Name == other.Name && Description == other.Description;

		public override int GetHashCode()
			=> (Name ?? string.Empty).GetHashCode();

		public static bool operator ==(TypeParameterDoc a, TypeParameterDoc b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(TypeParameterDoc a, TypeParameterDoc b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public override string ToString()
			=> Name ?? string.Empty;
	}
}
