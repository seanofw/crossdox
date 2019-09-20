using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Crossdox.Extensions;

namespace Crossdox.DocTypes
{
	public class MethodDoc : IEquatable<MethodDoc>
	{
		public NameInfo Name { get; }
		public MetaDoc Meta { get; }
		public ImmutableList<ParameterDoc> Parameters { get; }
		public ImmutableList<TypeParameterDoc> TypeParameters { get; }
		public ImmutableList<ExceptionDoc> Exceptions { get; }

		public string ShortName
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();

				if ((Name.Flags & (NameFlags.Constructor | NameFlags.ClassConstructor)) != 0
					&& Name.Classes.Any())
				{
					ClassInfo @class = Name.Classes.Last();
					stringBuilder.Append(@class.Name);
				}
				else
				{
					stringBuilder.Append(Name.Name);
				}

				if (TypeParameters != null && TypeParameters.Any())
				{
					stringBuilder.Append("<");
					stringBuilder.Append(string.Join(", ", TypeParameters.Select(p => p.Name)));
					stringBuilder.Append(">");
				}

				stringBuilder.Append("(");
				stringBuilder.Append(string.Join(", ", Parameters.Select(p => p.Name)));
				stringBuilder.Append(")");

				if ((Name.Flags & NameFlags.Static) != 0)
					stringBuilder.Append(" [static]");

				return stringBuilder.ToString();
			}
		}

		public MethodDoc(NameInfo name, MetaDoc meta = null,
			IEnumerable<TypeParameterDoc> typeParameters = null,
			IEnumerable<ParameterDoc> parameters = null,
			IEnumerable<ExceptionDoc> exceptions = null)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Meta = meta;
			Parameters = parameters.MakeImmutableList();
			TypeParameters = typeParameters.MakeImmutableList();
			Exceptions = exceptions.MakeImmutableList();
		}

		public MethodDoc MergeDocumentation(MethodDoc documentedMethod)
			=> WithMeta(documentedMethod.Meta)
				.WithExceptions(documentedMethod.Exceptions)
				.WithParameters(MergeParameters(Parameters, documentedMethod.Parameters))
				.WithTypeParameters(MergeTypeParameters(TypeParameters, documentedMethod.TypeParameters));

		private IEnumerable<ParameterDoc> MergeParameters(
			IList<ParameterDoc> reflectedParameters,
			IList<ParameterDoc> documentedParameters)
		{
			List<ParameterDoc> result = new List<ParameterDoc>();

			Dictionary<string, int> lookup = reflectedParameters
				.Select((t, index) => new KeyValuePair<string, int>(t.Name, index))
				.ToDictionary(p => p.Key, p => p.Value);

			result.AddRange(reflectedParameters);

			foreach (ParameterDoc parameterDoc in documentedParameters)
			{
				if (lookup.TryGetValue(parameterDoc.Name, out int index))
				{
					ParameterDoc reflectedParameterDoc = result[index];
					result[index] = new ParameterDoc(reflectedParameterDoc.Name,
						reflectedParameterDoc.ParameterType, reflectedParameterDoc.ParameterKind,
						parameterDoc.Description);
				}
				else result.Add(parameterDoc);
			}

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

		public MethodDoc WithName(NameInfo name)
			=> new MethodDoc(name, Meta, TypeParameters, Parameters, Exceptions);
		public MethodDoc WithMeta(MetaDoc meta)
			=> new MethodDoc(Name, meta, TypeParameters, Parameters, Exceptions);
		public MethodDoc WithParameters(IEnumerable<ParameterDoc> parameters)
			=> new MethodDoc(Name, Meta, TypeParameters, parameters, Exceptions);
		public MethodDoc WithTypeParameters(IEnumerable<TypeParameterDoc> typeParameters)
			=> new MethodDoc(Name, Meta, typeParameters, Parameters, Exceptions);
		public MethodDoc WithExceptions(IEnumerable<ExceptionDoc> exceptions)
			=> new MethodDoc(Name, Meta, TypeParameters, Parameters, exceptions);

		public MethodDoc AddParameter(ParameterDoc parameter)
			=> WithParameters(Parameters.Add(parameter));
		public MethodDoc AddTypeParameter(TypeParameterDoc typeParameter)
			=> WithTypeParameters(TypeParameters.Add(typeParameter));
		public MethodDoc AddException(ExceptionDoc exception)
			=> WithExceptions(Exceptions.Add(exception));

		public override bool Equals(object obj)
			=> Equals(obj as MethodDoc);
		public bool Equals(MethodDoc other)
			=> !ReferenceEquals(other, null)
				&& Name == other.Name && Meta == other.Meta
				&& Parameters.OrderBy(p => p.Name).SequenceEqual(other.Parameters.OrderBy(p => p.Name))
				&& TypeParameters.OrderBy(p => p.Name).SequenceEqual(other.TypeParameters.OrderBy(p => p.Name))
				&& Exceptions.SequenceEqual(other.Exceptions);

		public override int GetHashCode()
			=> Name.GetHashCode();

		public static bool operator ==(MethodDoc a, MethodDoc b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(MethodDoc a, MethodDoc b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public override string ToString()
			=> Name.ToString();
	}
}
