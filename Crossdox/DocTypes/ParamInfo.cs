using System;
using System.Text;

namespace Crossdox.DocTypes
{
	public class ParamInfo : IEquatable<ParamInfo>
	{
		public PopulatedType Type { get; }
		public string Name { get; }
		public ParameterKind ParameterKind { get; }

		private readonly string _stringified;

		public ParamInfo(PopulatedType type, string name = null, ParameterKind parameterKind = default)
		{
			Type = type;
			Name = name ?? string.Empty;
			ParameterKind = parameterKind;

			_stringified = Stringify();
		}

		public ParamInfo WithType(PopulatedType type)
			=> new ParamInfo(type, Name, ParameterKind);
		public ParamInfo WithName(string name)
			=> new ParamInfo(Type, name, ParameterKind);
		public ParamInfo WithKind(ParameterKind parameterKind)
			=> new ParamInfo(Type, Name, parameterKind);
		public ParamInfo ReplaceTypeParameterNames(Func<string, string> replacer)
			=> new ParamInfo(Type.ReplaceTypeParameterNames(replacer), Name, ParameterKind);

		public override string ToString()
			=> _stringified;

		private string Stringify()
		{
			StringBuilder stringBuilder = new StringBuilder();

			bool isFirst = true;

			if ((ParameterKind & ParameterKind.Out) != 0)
			{
				stringBuilder.Append("out");
				isFirst = false;
			}
			else if ((ParameterKind & ParameterKind.Ref) != 0)
			{
				stringBuilder.Append("ref");
				isFirst = false;
			}

			if (Type != null)
			{
				if (!isFirst)
					stringBuilder.Append(" ");
				stringBuilder.Append(Type);
				isFirst = false;
			}

			if (!string.IsNullOrEmpty(Name))
			{
				if (!isFirst)
					stringBuilder.Append(" ");
				stringBuilder.Append(Name);
			}

			return stringBuilder.ToString();
		}

		public override bool Equals(object obj)
			=> Equals(obj as ParamInfo);
		public bool Equals(ParamInfo other)
			=> !ReferenceEquals(other, null)
				&& _stringified == other._stringified;
		public override int GetHashCode()
			=> _stringified.GetHashCode();

		public static bool operator ==(ParamInfo a, ParamInfo b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(ParamInfo a, ParamInfo b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);
	}
}