using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Markdox.DocTypes
{
	public class NameInfo : IEquatable<NameInfo>
	{
		public IList<ClassInfo> Classes { get; }
		public string Name { get; }
		public IList<string> TypeParameters { get; }
		public IList<ParamInfo> Parameters { get; }
		public NameFlags Flags { get; }

		private string _stringified;

		public NameInfo(IEnumerable<ClassInfo> classes,
			string name, IEnumerable<string> typeParameters, IEnumerable<ParamInfo> parameters,
			NameFlags flags)
		{
			Classes = new ReadOnlyCollection<ClassInfo>(classes != null ? classes.ToArray() : new ClassInfo[0]);
			Name = name ?? string.Empty;
			TypeParameters = new ReadOnlyCollection<string>(typeParameters != null ? typeParameters.ToArray() : new string[0]);
			Parameters = new ReadOnlyCollection<ParamInfo>(parameters != null ? parameters.ToArray() : new ParamInfo[0]);
			Flags = flags;

			_stringified = Stringify(true, false, true);
		}

		public string ClrName
			=> TypeParameters.Count > 0 ? Name + ((Flags & NameFlags.Method) != 0 ? "``" : "`") + TypeParameters.Count : Name;

		public NameInfo Parent
		{
			get
			{
				if (!Classes.Any())
					return null;

				ClassInfo lastClassInfo = Classes.Last();
				return new NameInfo(Classes.Take(Classes.Count - 1),
					lastClassInfo.Name, lastClassInfo.TypeParameters, null, 0);
			}
		}

		public string Container
			=> Parent.ToString();

		public string NameWithParameters
			=> Stringify(false, false, true);

		public NameInfo GenericizeTypeParameters()
		{
			Dictionary<string, string> newTypeParameters = new Dictionary<string, string>();
			List<ClassInfo> newClasses = new List<ClassInfo>();

			foreach (ClassInfo classInfo in Classes)
			{
				List<string> classTypeParameters = new List<string>();
				foreach (string typeParameter in classInfo.TypeParameters)
				{
					string newTypeParameter = "T" + (newTypeParameters.Count + 1);
					classTypeParameters.Add(newTypeParameter);
					newTypeParameters.Add(typeParameter, newTypeParameter);
				}
				newClasses.Add(new ClassInfo(classInfo.Name, classTypeParameters, classInfo.Flags));
			}

			List<string> finalTypeParameters = new List<string>();
			foreach (string typeParameter in TypeParameters)
			{
				string newTypeParameter = "T" + (newTypeParameters.Count + 1);
				finalTypeParameters.Add(newTypeParameter);
				newTypeParameters.Add(typeParameter, newTypeParameter);
			}

			List<ParamInfo> newParameters = new List<ParamInfo>();
			foreach (ParamInfo param in Parameters)
			{
				newParameters.Add(param.ReplaceTypeParameterNames(oldName =>
					newTypeParameters.TryGetValue(oldName, out string newName)
						? newName
						: oldName
				).WithName(null).WithKind(0));
			}

			return new NameInfo(newClasses, Name, finalTypeParameters, newParameters, Flags);
		}

		public IEnumerable<ClassInfo> AsClass
		{
			get
			{
				List<ClassInfo> classes = new List<ClassInfo>();
				classes.AddRange(Classes);
				classes.Add(new ClassInfo(Name, TypeParameters, Flags));
				return classes;
			}
		}

		public NameInfo ReplaceTypeParameterNames(Func<string, string> replacer)
			=> new NameInfo(Classes.Select(c => c.ReplaceTypeParameterNames(replacer)),
				Name,
				TypeParameters.Select(t => replacer(t)),
				Parameters.Select(p => p.ReplaceTypeParameterNames(replacer)),
				Flags);

		public NameInfo WithClasses(IEnumerable<ClassInfo> classes)
			=> new NameInfo(classes, Name, TypeParameters, Parameters, Flags);
		public NameInfo WithName(string name)
			=> new NameInfo(Classes, name, TypeParameters, Parameters, Flags);
		public NameInfo WithTypeParameters(IEnumerable<string> typeParameters)
			=> new NameInfo(Classes, Name, typeParameters, Parameters, Flags);
		public NameInfo WithParameters(IEnumerable<ParamInfo> parameters)
			=> new NameInfo(Classes, Name, TypeParameters, parameters, Flags);
		public NameInfo WithIsMethod(bool isMethod)
			=> new NameInfo(Classes, Name, TypeParameters, Parameters, Flags);

		public override string ToString()
			=> _stringified;

		public string Stringify(bool includeClasses, bool includeModifiers, bool includeMethodParameters)
		{
			StringBuilder stringBuilder = new StringBuilder();

			if (includeModifiers)
			{
				if ((Flags & NameFlags.Private) != 0)	stringBuilder.Append("private ");
				if ((Flags & NameFlags.Protected) != 0)	stringBuilder.Append("protected ");
				if ((Flags & NameFlags.Internal) != 0)	stringBuilder.Append("internal ");
				if ((Flags & NameFlags.Public) != 0)	stringBuilder.Append("public ");

				if ((Flags & NameFlags.Static) != 0)	stringBuilder.Append("static ");
				if ((Flags & NameFlags.Sealed) != 0)	stringBuilder.Append("sealed ");
				if ((Flags & NameFlags.Abstract) != 0)	stringBuilder.Append("abstract ");
				if ((Flags & NameFlags.Virtual) != 0)	stringBuilder.Append("virtual ");
				if ((Flags & NameFlags.New) != 0)		stringBuilder.Append("new ");

				if ((Flags & NameFlags.Const) != 0)		stringBuilder.Append("const ");
				if ((Flags & NameFlags.ReadOnly) != 0)	stringBuilder.Append("readonly ");
			}

			bool isFirst = true;

			if (includeClasses)
			{
				foreach (ClassInfo @class in Classes)
				{
					if (!isFirst)
					{
						stringBuilder.Append('.');
					}
					stringBuilder.Append(@class);
					isFirst = false;
				}

				if (!isFirst)
				{
					stringBuilder.Append('.');
				}
			}

			if ((Flags & (NameFlags.Constructor | NameFlags.ClassConstructor)) != 0
				&& Classes.Any())
			{
				ClassInfo @class = Classes.Last();
				stringBuilder.Append(@class.Name);
			}
			else
			{
				stringBuilder.Append(Name);
			}

			AppendTypeParameters(stringBuilder, TypeParameters);

			if ((Flags & (NameFlags.Method | NameFlags.ClassConstructor | NameFlags.Constructor)) != 0
				&& includeMethodParameters)
			{
				stringBuilder.Append('(');
				AppendParameters(stringBuilder, Parameters);
				stringBuilder.Append(')');
			}

			return stringBuilder.ToString();
		}

		internal static void AppendTypeParameters(StringBuilder stringBuilder, IEnumerable<string> typeParameters)
		{
			if (typeParameters == null || !typeParameters.Any())
				return;

			stringBuilder.Append('<');
			bool isFirst = true;
			foreach (string typeParameter in typeParameters)
			{
				if (!isFirst)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(typeParameter);
				isFirst = false;
			}
			stringBuilder.Append('>');
		}

		private static void AppendParameters(StringBuilder stringBuilder, IEnumerable<ParamInfo> parameters)
		{
			if (parameters == null || !parameters.Any())
				return;

			bool isFirst = true;
			foreach (ParamInfo parameter in parameters)
			{
				if (!isFirst)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(parameter);
				isFirst = false;
			}
		}

		public override bool Equals(object obj)
			=> Equals(obj as NameInfo);
		public bool Equals(NameInfo other)
			=> !ReferenceEquals(other, null)
				&& _stringified == other._stringified;
		public override int GetHashCode()
			=> _stringified.GetHashCode();

		public static bool operator ==(NameInfo a, NameInfo b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(NameInfo a, NameInfo b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public static implicit operator string(NameInfo nameInfo)
			=> nameInfo.ToString();
	}
}
