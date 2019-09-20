using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Markdox.Extensions;

namespace Markdox.DocTypes
{
	public class PropertyDoc : IEquatable<PropertyDoc>
	{
		public NameInfo Name { get; }
		public MetaDoc Meta { get; }
		public ImmutableList<ExceptionDoc> Exceptions { get; }
		public NameFlags GetterFlags { get; }
		public NameFlags SetterFlags { get; }

		public PropertyDoc(NameInfo name, MetaDoc meta = null, IEnumerable<ExceptionDoc> exceptions = null,
			NameFlags getterFlags = default, NameFlags setterFlags = default)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Meta = meta;
			Exceptions = exceptions.MakeImmutableList();
			GetterFlags = getterFlags;
			SetterFlags = setterFlags;
		}

		public PropertyDoc MergeDocumentation(PropertyDoc documentedProperty)
			=> WithMeta(documentedProperty.Meta)
				.WithExceptions(documentedProperty.Exceptions);

		public PropertyDoc WithName(NameInfo name)
			=> new PropertyDoc(name, Meta, Exceptions, GetterFlags, SetterFlags);
		public PropertyDoc WithMeta(MetaDoc meta)
			=> new PropertyDoc(Name, meta, Exceptions, GetterFlags, SetterFlags);
		public PropertyDoc WithExceptions(IEnumerable<ExceptionDoc> exceptions)
			=> new PropertyDoc(Name, Meta, exceptions, GetterFlags, SetterFlags);
		public PropertyDoc WithGetterFlags(NameFlags getterFlags)
			=> new PropertyDoc(Name, Meta, Exceptions, getterFlags, SetterFlags);
		public PropertyDoc WithSetterFlags(NameFlags setterFlags)
			=> new PropertyDoc(Name, Meta, Exceptions, GetterFlags, setterFlags);
		public PropertyDoc AddException(ExceptionDoc exception)
			=> WithExceptions(Exceptions.Add(exception));

		public override bool Equals(object obj)
			=> Equals(obj as PropertyDoc);
		public bool Equals(PropertyDoc other)
			=> other != null
				&& Name == other.Name && Meta == other.Meta
				&& GetterFlags == other.GetterFlags && SetterFlags == other.SetterFlags
				&& Exceptions.SequenceEqual(other.Exceptions);

		public override int GetHashCode()
			=> Name.GetHashCode();

		public static bool operator ==(PropertyDoc a, PropertyDoc b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(PropertyDoc a, PropertyDoc b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public override string ToString()
			=> Name.ToString();
	}
}