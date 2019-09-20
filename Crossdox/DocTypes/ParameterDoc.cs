using System;

namespace Crossdox.DocTypes
{
	public class ParameterDoc : IEquatable<ParameterDoc>
	{
		public string Name { get; }
		public NameInfo ParameterType { get; }
		public ParameterKind ParameterKind { get; }
		public string Description { get; }

		public ParameterDoc(string name, NameInfo parameterType = null,
			ParameterKind parameterKind = ParameterKind.In, string description = null)
		{
			Name = name;
			ParameterType = parameterType;
			ParameterKind = parameterKind;
			Description = description;
		}

		public ParameterDoc WithName(string name)
			=> new ParameterDoc(name, ParameterType, ParameterKind, Description);
		public ParameterDoc WithParameterType(NameInfo parameterType)
			=> new ParameterDoc(Name, parameterType, ParameterKind, Description);
		public ParameterDoc WithDescription(string description)
			=> new ParameterDoc(Name, ParameterType, ParameterKind, description);
		public ParameterDoc WithParameterKind(ParameterKind parameterKind)
			=> new ParameterDoc(Name, ParameterType, parameterKind, Description);

		public override bool Equals(object obj)
			=> Equals(obj as ParameterDoc);
		public bool Equals(ParameterDoc other)
			=> other != null
				&& Name == other.Name && ParameterType == other.ParameterType
				&& Description == other.Description;

		public override int GetHashCode()
			=> (Name ?? string.Empty).GetHashCode();

		public static bool operator ==(ParameterDoc a, ParameterDoc b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(ParameterDoc a, ParameterDoc b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public override string ToString()
			=> $"{ParameterType} {Name}";
	}
}
