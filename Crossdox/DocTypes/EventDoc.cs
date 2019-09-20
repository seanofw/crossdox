using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Crossdox.Extensions;

namespace Crossdox.DocTypes
{
	public class EventDoc : IEquatable<EventDoc>
	{
		public NameInfo Name { get; }
		public MetaDoc Meta { get; }
		public ImmutableList<ExceptionDoc> Exceptions { get; }
		public NameFlags AdderFlags { get; }
		public NameFlags RemoverFlags { get; }

		public EventDoc(NameInfo name, MetaDoc meta = null, IEnumerable<ExceptionDoc> exceptions = null,
			NameFlags adderFlags = default, NameFlags removerFlags = default)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Meta = meta;
			Exceptions = exceptions.MakeImmutableList();
			AdderFlags = adderFlags;
			RemoverFlags = removerFlags;
		}

		public EventDoc MergeDocumentation(EventDoc documentedEvent)
			=> WithMeta(documentedEvent.Meta)
				.WithExceptions(documentedEvent.Exceptions);

		public EventDoc WithName(NameInfo name)
			=> new EventDoc(name, Meta, Exceptions, AdderFlags, RemoverFlags);
		public EventDoc WithMeta(MetaDoc meta)
			=> new EventDoc(Name, meta, Exceptions, AdderFlags, RemoverFlags);
		public EventDoc WithExceptions(IEnumerable<ExceptionDoc> exceptions)
			=> new EventDoc(Name, Meta, exceptions, AdderFlags, RemoverFlags);
		public EventDoc WithAdderFlags(NameFlags adderFlags)
			=> new EventDoc(Name, Meta, Exceptions, adderFlags, RemoverFlags);
		public EventDoc WithRemoverFlags(NameFlags removerFlags)
			=> new EventDoc(Name, Meta, Exceptions, AdderFlags, removerFlags);
		public EventDoc AddException(ExceptionDoc exception)
			=> WithExceptions(Exceptions.Add(exception));

		public override bool Equals(object obj)
			=> Equals(obj as EventDoc);
		public bool Equals(EventDoc other)
			=> other != null
				&& Name == other.Name && Meta == other.Meta
				&& AdderFlags == other.AdderFlags && RemoverFlags == other.RemoverFlags
				&& Exceptions.SequenceEqual(other.Exceptions);

		public override int GetHashCode()
			=> Name.GetHashCode();

		public static bool operator ==(EventDoc a, EventDoc b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(EventDoc a, EventDoc b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public override string ToString()
			=> Name.ToString();
	}
}