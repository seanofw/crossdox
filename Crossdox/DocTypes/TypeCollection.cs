using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Crossdox.DocTypes
{
	public class TypeCollection : ICollection<TypeDoc>, IEquatable<TypeCollection>
	{
		public ImmutableDictionary<NameInfo, TypeDoc> Types { get; }

		public TypeCollection(ImmutableDictionary<NameInfo, TypeDoc> types = null)
		{
			Types = types ?? ImmutableDictionary<NameInfo, TypeDoc>.Empty;
		}

		public TypeCollection WithTypes(ImmutableDictionary<NameInfo, TypeDoc> types)
			=> new TypeCollection(types);
		public TypeCollection AddType(NameInfo name, TypeDoc type)
			=> WithTypes(Types.Add(name, type));

		public TypeCollection AddTypeRange(IEnumerable<TypeDoc> types)
		{
			ImmutableDictionary<NameInfo, TypeDoc> typeDictionary = Types;

			foreach (TypeDoc type in types)
			{
				typeDictionary = typeDictionary.Add(type.Name, type);
			}

			return new TypeCollection(typeDictionary);
		}

		public TypeCollection MergeInDocumentation(TypeCollection documentationCollection)
		{
			TypeCollection result = this;

			HashSet<NameInfo> appliedTypes = new HashSet<NameInfo>();

			foreach (TypeDoc type in this)
			{
				NameInfo name = type.Name;
				NameInfo genericizedName = name.GenericizeTypeParameters();
				if (documentationCollection.Types.TryGetValue(genericizedName, out TypeDoc typeDoc))
				{
					TypeDoc newType = type.MergeInDocumentation(typeDoc);
					result = new TypeCollection(result.Types.Remove(genericizedName).SetItem(name, newType));
					appliedTypes.Add(genericizedName);
				}
			}

			foreach (TypeDoc type in documentationCollection)
			{
				if (appliedTypes.Contains(type.Name))
					continue;
				result = new TypeCollection(result.Types.Add(type.Name, type));
			}

			return result;
		}

		public override bool Equals(object obj)
			=> Equals(obj as TypeCollection);
		public bool Equals(TypeCollection other)
			=> Types.OrderBy(t => t.Key).SequenceEqual(other.Types.OrderBy(t => t.Key));

		public override int GetHashCode()
			=> string.Join(",", Types.Keys.OrderBy(k => k)).GetHashCode();

		public static bool operator ==(TypeCollection a, TypeCollection b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(TypeCollection a, TypeCollection b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public override string ToString()
			=> $"TypeCollection of {Types.Count} types";

		#region ICollection

		public int Count => Types.Count;

		public bool IsReadOnly => true;

		public void Add(TypeDoc item)
			=> throw new NotSupportedException();

		public void Clear()
			=> throw new NotSupportedException();

		public bool Contains(TypeDoc item)
			=> Types.ContainsKey(item.Name);

		public void CopyTo(TypeDoc[] array, int arrayIndex)
		{
			foreach (TypeDoc typeDoc in Types.Values.OrderBy(t => t.Name.ToString()))
			{
				array[arrayIndex++] = typeDoc;
			}
		}

		public bool Remove(TypeDoc item)
			=> throw new NotSupportedException();

		public IEnumerator<TypeDoc> GetEnumerator()
			=> Types.Values.OrderBy(t => t.Name.ToString()).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		#endregion
	}
}
