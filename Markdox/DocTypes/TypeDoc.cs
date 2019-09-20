using Markdox.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Markdox.DocTypes
{
	public class TypeDoc : IEquatable<TypeDoc>
	{
		public NameInfo Name { get; }
		public MetaDoc Meta { get; }
		public ImmutableList<TypeParameterDoc> TypeParameters { get; }
		public ImmutableDictionary<string, FieldDoc> Fields { get; }
		public ImmutableDictionary<string, PropertyDoc> Properties { get; }
		public ImmutableDictionary<string, EventDoc> Events { get; }
		public ImmutableDictionary<string, MethodDoc> Methods { get; }

		public TypeDoc(NameInfo name, MetaDoc meta = null,
			IEnumerable<TypeParameterDoc> typeParameters = null,
			ImmutableDictionary<string, FieldDoc> fields = null,
			ImmutableDictionary<string, PropertyDoc> properties = null,
			ImmutableDictionary<string, EventDoc> events = null,
			ImmutableDictionary<string, MethodDoc> methods = null)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Meta = meta;
			TypeParameters = typeParameters.MakeImmutableList();
			Fields = fields ?? ImmutableDictionary<string, FieldDoc>.Empty;
			Properties = properties ?? ImmutableDictionary<string, PropertyDoc>.Empty;
			Events = events ?? ImmutableDictionary<string, EventDoc>.Empty;
			Methods = methods ?? ImmutableDictionary<string, MethodDoc>.Empty;
		}

		public TypeDoc WithName(NameInfo name)
			=> new TypeDoc(name, Meta, TypeParameters, Fields, Properties, Events, Methods);
		public TypeDoc WithMeta(MetaDoc meta)
			=> new TypeDoc(Name, meta, TypeParameters, Fields, Properties, Events, Methods);
		public TypeDoc WithTypeParameters(IEnumerable<TypeParameterDoc> typeParameters)
			=> new TypeDoc(Name, Meta, typeParameters, Fields, Properties, Events, Methods);
		public TypeDoc WithFields(ImmutableDictionary<string, FieldDoc> fields)
			=> new TypeDoc(Name, Meta, TypeParameters, fields, Properties, Events, Methods);
		public TypeDoc WithProperties(ImmutableDictionary<string, PropertyDoc> properties)
			=> new TypeDoc(Name, Meta, TypeParameters, Fields, properties, Events, Methods);
		public TypeDoc WithEvents(ImmutableDictionary<string, EventDoc> events)
			=> new TypeDoc(Name, Meta, TypeParameters, Fields, Properties, events, Methods);
		public TypeDoc WithMethods(ImmutableDictionary<string, MethodDoc> methods)
			=> new TypeDoc(Name, Meta, TypeParameters, Fields, Properties, Events, methods);

		public TypeDoc AddTypeParameter(TypeParameterDoc typeParameter)
			=> WithTypeParameters(TypeParameters.Add(typeParameter));
		public TypeDoc AddField(string name, FieldDoc field)
			=> WithFields(Fields.Add(name, field));
		public TypeDoc AddProperty(string name, PropertyDoc property)
			=> WithProperties(Properties.Add(name, property));
		public TypeDoc AddEvent(string name, EventDoc @event)
			=> WithEvents(Events.Add(name, @event));
		public TypeDoc AddMethod(string name, MethodDoc method)
			=> WithMethods(Methods.Add(name, method));

		public TypeDoc MergeInDocumentation(TypeDoc documentation)
		{
			TypeDoc result = this;

			if (documentation.Meta != null)
				result = result.WithMeta(documentation.Meta);

			if (documentation.TypeParameters != null && documentation.TypeParameters.Any())
				result = result.WithTypeParameters(MergeTypeParameters(TypeParameters, documentation.TypeParameters));

			if (documentation.Fields != null && documentation.Fields.Any())
				result = result.WithFields(MergeCollection(Fields, documentation.Fields,
					field => field.Name,
					reflectedField => reflectedField.Name.GenericizeTypeParameters().NameWithParameters,
					(reflectedField, documentedField) => reflectedField.MergeDocumentation(documentedField)));

			if (documentation.Properties != null && documentation.Properties.Any())
				result = result.WithProperties(MergeCollection(Properties, documentation.Properties,
					property => property.Name,
					reflectedProperty => reflectedProperty.Name.GenericizeTypeParameters().NameWithParameters,
					(reflectedProperty, documentedProperty) => reflectedProperty.MergeDocumentation(documentedProperty)));

			if (documentation.Events != null && documentation.Events.Any())
				result = result.WithEvents(MergeCollection(Events, documentation.Events,
					@event => @event.Name,
					reflectedEvent => reflectedEvent.Name.GenericizeTypeParameters().NameWithParameters,
					(reflectedEvent, documentedEvent) => reflectedEvent.MergeDocumentation(documentedEvent)));

			if (documentation.Methods != null && documentation.Methods.Any())
				result = result.WithMethods(MergeCollection(Methods, documentation.Methods,
					method => method.Name,
					reflectedMethod => reflectedMethod.Name.GenericizeTypeParameters().NameWithParameters,
					(reflectedMethod, documentedMethod) => reflectedMethod.MergeDocumentation(documentedMethod)));

			return result;
		}

		private IEnumerable<TypeParameterDoc> MergeTypeParameters(
			IList<TypeParameterDoc> reflectedTypeParameters,
			IList<TypeParameterDoc> documentedTypeParameters)
		{
			List<TypeParameterDoc> result = new List<TypeParameterDoc>();

			Dictionary<string, int> lookup = reflectedTypeParameters
				.Select((t, index) => new KeyValuePair<string, int>(t.Name, index))
				.ToDictionary(p => p.Key, p => p.Value);

			result.AddRange(reflectedTypeParameters);

			foreach (TypeParameterDoc typeParameterDoc in documentedTypeParameters)
			{
				if (lookup.TryGetValue(typeParameterDoc.Name, out int index))
				{
					result[index] = typeParameterDoc;
				}
				else result.Add(typeParameterDoc);
			}

			return result;
		}

		private ImmutableDictionary<string, T> MergeCollection<T>(
			ImmutableDictionary<string, T> reflectedItems,
			IDictionary<string, T> documentedItems,
			Func<T, string> getName,
			Func<T, string> genericizeReflectedName,
			Func<T, T, T> merge)
		{
			Dictionary<string, T> reflectedLookup = new Dictionary<string, T>();
			foreach (KeyValuePair<string, T> pair in reflectedItems)
			{
				reflectedLookup.Add(genericizeReflectedName(pair.Value), pair.Value);
			}

			Dictionary<string, T> result = new Dictionary<string, T>();
			foreach (KeyValuePair<string, T> pair in documentedItems)
			{
				string genericDocumentedName = genericizeReflectedName(pair.Value);
				if (reflectedLookup.TryGetValue(genericDocumentedName, out T reflectedItem))
				{
					T combinedItem = merge(reflectedItem, pair.Value);
					result.Remove(genericDocumentedName);
					result[getName(combinedItem)] = combinedItem;
				}
				else
				{
					result[getName(pair.Value)] = pair.Value;
				}
			}

			return ImmutableDictionary<string, T>.Empty.AddRange(result);
		}

		public override bool Equals(object obj)
			=> Equals(obj as TypeDoc);
		public bool Equals(TypeDoc other)
			=> other != null
				&& Name == other.Name && Meta == other.Meta
				&& TypeParameters.OrderBy(p => p.Name).SequenceEqual(other.TypeParameters.OrderBy(p => p.Name))
				&& Fields.OrderBy(p => p.Key).SequenceEqual(other.Fields.OrderBy(p => p.Key))
				&& Properties.OrderBy(p => p.Key).SequenceEqual(other.Properties.OrderBy(p => p.Key))
				&& Events.OrderBy(p => p.Key).SequenceEqual(other.Events.OrderBy(p => p.Key))
				&& Methods.OrderBy(p => p.Key).SequenceEqual(other.Methods.OrderBy(p => p.Key));

		public override int GetHashCode()
			=> Name.GetHashCode();

		public static bool operator ==(TypeDoc a, TypeDoc b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(TypeDoc a, TypeDoc b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public override string ToString()
			=> Name.ToString();
	}
}
