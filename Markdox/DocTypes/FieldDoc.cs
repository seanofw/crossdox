using System;

namespace Markdox.DocTypes
{
	public class FieldDoc : IEquatable<FieldDoc>
	{
		public NameInfo Name { get; }
		public MetaDoc Meta { get; }

		public FieldDoc(NameInfo name, MetaDoc meta = null)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Meta = meta;
		}

		public FieldDoc MergeDocumentation(FieldDoc documentedField)
			=> WithMeta(documentedField.Meta);

		public FieldDoc WithName(NameInfo name)
			=> new FieldDoc(name, Meta);
		public FieldDoc WithMeta(MetaDoc meta)
			=> new FieldDoc(Name, meta);

		public override bool Equals(object obj)
			=> Equals(obj as FieldDoc);
		public bool Equals(FieldDoc other)
			=> other != null
				&& Name == other.Name && Meta == other.Meta;

		public override int GetHashCode()
			=> Name.GetHashCode();

		public static bool operator ==(FieldDoc a, FieldDoc b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(FieldDoc a, FieldDoc b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public override string ToString()
			=> Name.ToString();
	}
}
