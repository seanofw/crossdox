using Markdox.DocTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Markdox.Reflection
{
	public class TypeLoader
	{
		public TypeCollection ReflectOnAssembly(Assembly assembly, IncludeKind includeKind)
		{
			TypeCollection typeCollection = new TypeCollection();

			Type[] types = assembly.GetTypes();
			foreach (Type type in types)
			{
				// Skip any classes they don't want us to include.
				IncludeKind currentIncludeKind = type.IsPublic ? IncludeKind.Public : IncludeKind.Internal;
				if ((currentIncludeKind & includeKind) == 0)
					continue;

				// Skip compiler-generated classes, which are invisible to both
				// consumers of the assembly and to the assembly's creator.
				if (type.Name.StartsWith("<"))
					continue;

				TypeDoc typeDoc = ConvertTypeToDocForm(type, includeKind);
				typeCollection = typeCollection.AddType(typeDoc.Name, typeDoc);
			}

			return typeCollection;
		}

		private TypeDoc ConvertTypeToDocForm(Type type, IncludeKind includeKind)
		{
			NameInfo typeName = ConvertTypeToNameInfo(type);

			IEnumerable<TypeParameterDoc> typeParameters = ExtractTypeParameters(type);
			ImmutableDictionary<string, FieldDoc> fields = ExtractFields(type, typeName, includeKind);
			ImmutableDictionary<string, PropertyDoc> properties = ExtractProperties(type, typeName, includeKind);
			ImmutableDictionary<string, EventDoc> events = ExtractEvents(type, typeName, includeKind);
			ImmutableDictionary<string, MethodDoc> methods = ExtractMethods(type, typeName, includeKind);
			ImmutableDictionary<string, MethodDoc> constructors = ExtractConstructors(type, typeName, includeKind);

			methods = methods.AddRange(constructors);

			TypeDoc typeDoc = new TypeDoc(typeName, null,
				typeParameters, fields, properties, events, methods);
			return typeDoc;
		}

		private ImmutableDictionary<string, FieldDoc> ExtractFields(Type type, NameInfo typeName, IncludeKind includeKind)
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

			ImmutableDictionary<string, FieldDoc> dictionary = ImmutableDictionary<string, FieldDoc>.Empty;

			IEnumerable<ClassInfo> classes = typeName.AsClass;

			foreach (FieldInfo field in fields)
			{
				// Don't emit fields that were just inherited.
				if (field.DeclaringType != type)
					continue;

				// Skip compiler-generated fields, which are invisible to both
				// consumers of the assembly and to the assembly's creator.
				if (type.Name.StartsWith("<"))
					continue;

				NameFlags nameFlags = GetVisibilityNameFlags(field);
				NameInfo fieldName = new NameInfo(classes, field.Name, null, null, nameFlags);
				dictionary = dictionary.Add(field.Name, new FieldDoc(fieldName));
			}

			return dictionary;
		}

		private ImmutableDictionary<string, PropertyDoc> ExtractProperties(Type type, NameInfo typeName, IncludeKind includeKind)
		{
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

			ImmutableDictionary<string, PropertyDoc> dictionary = ImmutableDictionary<string, PropertyDoc>.Empty;

			IEnumerable<ClassInfo> classes = typeName.AsClass;

			foreach (PropertyInfo property in properties)
			{
				// Don't emit properties that were just inherited.
				if (property.DeclaringType != type)
					continue;

				// Skip compiler-generated properties, which are invisible to both
				// consumers of the assembly and to the assembly's creator.
				if (type.Name.StartsWith("<"))
					continue;

				NameFlags getterFlags = GetVisibilityNameFlags(property.GetGetMethod());
				NameFlags setterFlags = GetVisibilityNameFlags(property.GetSetMethod());

				NameFlags nameFlags = (getterFlags | setterFlags)
					& (NameFlags.Static | NameFlags.Abstract | NameFlags.Virtual | NameFlags.New);
				if (property.Name.StartsWith("#"))
					nameFlags |= NameFlags.SpecialName;
				else if (property.Name.Contains("#"))
					nameFlags |= NameFlags.ExplicitInterfaceImplementation;

				if (((getterFlags | setterFlags) & NameFlags.AllVisibilities) >= NameFlags.Public)
					nameFlags |= NameFlags.Public;
				else if (((getterFlags | setterFlags) & NameFlags.AllVisibilities) >= NameFlags.Internal)
					nameFlags |= NameFlags.Internal;
				else if (((getterFlags | setterFlags) & NameFlags.AllVisibilities) >= NameFlags.Protected)
					nameFlags |= NameFlags.Protected;
				else if (((getterFlags | setterFlags) & NameFlags.AllVisibilities) >= NameFlags.Private)
					nameFlags |= NameFlags.Private;

				NameInfo propertyName = new NameInfo(classes, property.Name, null, null, nameFlags);
				dictionary = dictionary.Add(property.Name, new PropertyDoc(propertyName,
					getterFlags: getterFlags, setterFlags: setterFlags));
			}

			return dictionary;
		}

		private ImmutableDictionary<string, EventDoc> ExtractEvents(Type type, NameInfo typeName, IncludeKind includeKind)
		{
			EventInfo[] events = type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

			ImmutableDictionary<string, EventDoc> dictionary = ImmutableDictionary<string, EventDoc>.Empty;

			IEnumerable<ClassInfo> classes = typeName.AsClass;

			foreach (EventInfo @event in events)
			{
				// Don't emit events that were just inherited.
				if (@event.DeclaringType != type)
					continue;

				// Skip compiler-generated events, which are invisible to both
				// consumers of the assembly and to the assembly's creator.
				if (type.Name.StartsWith("<"))
					continue;

				NameFlags adderFlags = GetVisibilityNameFlags(@event.GetAddMethod());
				NameFlags removerFlags = GetVisibilityNameFlags(@event.GetRemoveMethod());

				NameFlags nameFlags = (adderFlags | removerFlags)
					& (NameFlags.Static | NameFlags.Abstract | NameFlags.Virtual | NameFlags.New);
				if (@event.Name.StartsWith("#"))
					nameFlags |= NameFlags.SpecialName;
				else if (@event.Name.Contains("#"))
					nameFlags |= NameFlags.ExplicitInterfaceImplementation;

				if (((adderFlags | removerFlags) & NameFlags.AllVisibilities) >= NameFlags.Public)
					nameFlags |= NameFlags.Public;
				else if (((adderFlags | removerFlags) & NameFlags.AllVisibilities) >= NameFlags.Internal)
					nameFlags |= NameFlags.Internal;
				else if (((adderFlags | removerFlags) & NameFlags.AllVisibilities) >= NameFlags.Protected)
					nameFlags |= NameFlags.Protected;
				else if (((adderFlags | removerFlags) & NameFlags.AllVisibilities) >= NameFlags.Private)
					nameFlags |= NameFlags.Private;

				NameInfo eventName = new NameInfo(classes, @event.Name, null, null, nameFlags);
				dictionary = dictionary.Add(@event.Name, new EventDoc(eventName,
					adderFlags: adderFlags, removerFlags: removerFlags));
			}

			return dictionary;
		}

		private ImmutableDictionary<string, MethodDoc> ExtractMethods(Type type, NameInfo typeName, IncludeKind includeKind)
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

			ImmutableDictionary<string, MethodDoc> dictionary = ImmutableDictionary<string, MethodDoc>.Empty;

			IEnumerable<ClassInfo> classes = typeName.AsClass;

			foreach (MethodInfo method in methods)
			{
				// Don't emit methods that were just inherited.
				if (method.DeclaringType != type)
					continue;

				// Skip compiler-generated methods, which are invisible to both
				// consumers of the assembly and to the assembly's creator.
				if (method.Name.StartsWith("<"))
					continue;
				if (method.IsSpecialName)
					continue;

				NameFlags nameFlags = GetVisibilityNameFlags(method);

				List<TypeParameterDoc> typeParameters = null;
				if (method.IsGenericMethodDefinition)
				{
					Type[] genericArgs = method.GetGenericArguments();
					typeParameters = genericArgs.Select(t => new TypeParameterDoc(t.Name, null)).ToList();
				}

				string effectiveMethodName = method.Name;
				List<Tuple<ParamInfo, ParameterDoc>> parameters = GetParameters(method.GetParameters());

				NameInfo methodName = new NameInfo(classes, method.Name,
					typeParameters?.Select(t => t.Name),
					parameters.Select(p => p.Item1),
					nameFlags);

				List<ParameterDoc> parameterDocs = parameters.Select(p => p.Item2).ToList();

				dictionary = dictionary.Add(methodName.NameWithParameters,
					new MethodDoc(methodName, null, typeParameters, parameterDocs, null));
			}

			return dictionary;
		}

		private ImmutableDictionary<string, MethodDoc> ExtractConstructors(Type type, NameInfo typeName, IncludeKind includeKind)
		{
			ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

			ImmutableDictionary<string, MethodDoc> dictionary = ImmutableDictionary<string, MethodDoc>.Empty;

			IEnumerable<ClassInfo> classes = typeName.AsClass;

			foreach (ConstructorInfo constructor in constructors)
			{
				// Don't emit constructors that were just inherited.
				if (constructor.DeclaringType != type)
					continue;

				// Skip compiler-generated constructors, which are invisible to both
				// consumers of the assembly and to the assembly's creator.
				if (constructor.Name.StartsWith("<"))
					continue;

				NameFlags nameFlags = GetVisibilityNameFlags(constructor);

				List<TypeParameterDoc> typeParameters = null;

				string effectiveMethodName = constructor.Name;
				int backtickIndex = effectiveMethodName.IndexOf('`');
				if (backtickIndex >= 0)
					effectiveMethodName = effectiveMethodName.Substring(0, backtickIndex);

				List<Tuple<ParamInfo, ParameterDoc>> parameters = GetParameters(constructor.GetParameters());

				NameInfo methodName = new NameInfo(classes, effectiveMethodName,
					typeParameters?.Select(t => t.Name),
					parameters.Select(p => p.Item1),
					nameFlags);

				List<ParameterDoc> parameterDocs = parameters.Select(p => p.Item2).ToList();

				dictionary = dictionary.Add(methodName.NameWithParameters,
					new MethodDoc(methodName, null, typeParameters, parameterDocs, null));
			}

			return dictionary;
		}

		private List<Tuple<ParamInfo, ParameterDoc>> GetParameters(IEnumerable<ParameterInfo> parameters)
		{
			List<Tuple<ParamInfo, ParameterDoc>> result = new List<Tuple<ParamInfo, ParameterDoc>>();

			foreach (ParameterInfo parameter in parameters)
			{
				ParameterKind parameterKind = default;
				if (parameter.IsIn)				parameterKind |= ParameterKind.In;
				if (parameter.IsOut)			parameterKind |= ParameterKind.Out;
				if (parameter.IsOptional)		parameterKind |= ParameterKind.Optional;
				if (parameter.HasDefaultValue)	parameterKind |= ParameterKind.HasDefaultValue;
				if (parameter.ParameterType.IsByRef) parameterKind |= ParameterKind.Ref;

				ParamInfo paramInfo = new ParamInfo(ConvertTypeToPopulatedType(parameter.ParameterType),
					parameter.Name, parameterKind);
				ParameterDoc parameterDoc = new ParameterDoc(parameter.Name,
					ConvertTypeToNameInfo(parameter.ParameterType), parameterKind);

				result.Add(new Tuple<ParamInfo, ParameterDoc>(paramInfo, parameterDoc));
			}

			return result;
		}

		private static NameFlags GetVisibilityNameFlags(FieldInfo fieldInfo)
		{
			NameFlags nameFlags = 0;

			if (fieldInfo.IsLiteral)
				nameFlags |= NameFlags.Const;
			if (fieldInfo.IsInitOnly)
				nameFlags |= NameFlags.ReadOnly;
			if (fieldInfo.IsStatic)
				nameFlags |= NameFlags.Static;
			if (fieldInfo.IsAssembly)
				nameFlags |= NameFlags.Internal;
			if (fieldInfo.IsPrivate)
				nameFlags |= NameFlags.Private;
			if (fieldInfo.IsPublic)
				nameFlags |= NameFlags.Public;
			if (fieldInfo.IsFamily)
				nameFlags |= NameFlags.Protected;
			if (fieldInfo.IsFamilyOrAssembly)
				nameFlags |= NameFlags.Protected | NameFlags.Internal;
			if (fieldInfo.IsFamilyAndAssembly)
				nameFlags |= NameFlags.Private | NameFlags.Protected;

			return nameFlags;
		}

		private NameFlags GetVisibilityNameFlags(MethodInfo methodInfo)
		{
			if (methodInfo == null)
				return default;

			NameFlags nameFlags = NameFlags.Method;

			if (methodInfo.IsAbstract)
				nameFlags |= NameFlags.Abstract;
			if (methodInfo.IsVirtual)
				nameFlags |= NameFlags.Virtual;
			if (methodInfo.IsHideBySig)
				nameFlags |= NameFlags.New;
			if (methodInfo.IsConstructor && methodInfo.IsStatic)
				nameFlags |= NameFlags.ClassConstructor;
			else if (methodInfo.IsConstructor)
				nameFlags |= NameFlags.Constructor;

			if (methodInfo.IsStatic)
				nameFlags |= NameFlags.Static;
			if (methodInfo.IsAssembly)
				nameFlags |= NameFlags.Internal;
			if (methodInfo.IsPrivate)
				nameFlags |= NameFlags.Private;
			if (methodInfo.IsPublic)
				nameFlags |= NameFlags.Public;
			if (methodInfo.IsFamily)
				nameFlags |= NameFlags.Protected;
			if (methodInfo.IsFamilyOrAssembly)
				nameFlags |= NameFlags.Protected | NameFlags.Internal;
			if (methodInfo.IsFamilyAndAssembly)
				nameFlags |= NameFlags.Private | NameFlags.Protected;

			return nameFlags;
		}

		private NameFlags GetVisibilityNameFlags(ConstructorInfo constructorInfo)
		{
			if (constructorInfo == null)
				return default;

			NameFlags nameFlags = constructorInfo.IsStatic
				? NameFlags.ClassConstructor
				: NameFlags.Constructor;

			if (constructorInfo.IsStatic)
				nameFlags |= NameFlags.Static;
			if (constructorInfo.IsAssembly)
				nameFlags |= NameFlags.Internal;
			if (constructorInfo.IsPrivate)
				nameFlags |= NameFlags.Private;
			if (constructorInfo.IsPublic)
				nameFlags |= NameFlags.Public;
			if (constructorInfo.IsFamily)
				nameFlags |= NameFlags.Protected;
			if (constructorInfo.IsFamilyOrAssembly)
				nameFlags |= NameFlags.Protected | NameFlags.Internal;
			if (constructorInfo.IsFamilyAndAssembly)
				nameFlags |= NameFlags.Private | NameFlags.Protected;

			return nameFlags;
		}

		private IEnumerable<TypeParameterDoc> ExtractTypeParameters(Type type)
		{
			List<TypeParameterDoc> list = null;

			if (type.IsGenericTypeDefinition)
			{
				Type[] genericArgs = type.GetGenericArguments();
				list = genericArgs.Select(g => new TypeParameterDoc(g.Name, null)).ToList();
			}

			return list;
		}

		private PopulatedType ConvertTypeToPopulatedType(Type type)
		{
			if (type.IsGenericParameter)
				return new PopulatedType(new PopulatedName(type.Name));

			if (type.IsByRef || type.IsPointer)
			{
				return ConvertTypeToPopulatedType(type.GetElementType());
			}

			if (type.IsArray)
			{
				int dimensions = type.GetArrayRank();
				PopulatedType baseType = ConvertTypeToPopulatedType(type.GetElementType());

				List<ArrayInfo> arrays = new List<ArrayInfo>();
				if (baseType.Arrays.Any())
				{
					arrays = baseType.Arrays.ToList();
				}
				arrays.Add(new ArrayInfo(dimensions));

				return new PopulatedType(baseType.Names, arrays);
			}

			List<PopulatedName> names = new List<PopulatedName>();
			if (type.DeclaringType != null)
			{
				PopulatedType container = ConvertTypeToPopulatedType(type.DeclaringType);
				names.AddRange(container.Names);
			}
			else
			{
				string[] namespacePieces = type.Namespace.Split('.');
				names.AddRange(namespacePieces.Select(n => new PopulatedName(n)));
			}

			string name = type.Name;

			int backtickIndex;
			if ((backtickIndex = name.IndexOf('`')) >= 0)
				name = name.Substring(0, backtickIndex);

			List<PopulatedType> typeParameters = null;
			if (type.IsGenericType)
			{
				Type[] genericArgs = type.GetGenericArguments();
				typeParameters = new List<PopulatedType>();
				foreach (Type genericArg in genericArgs)
				{
					PopulatedType typeParameter = ConvertTypeToPopulatedType(genericArg);
					typeParameters.Add(typeParameter);
				}
			}

			PopulatedName populatedName = new PopulatedName(name, typeParameters);
			names.Add(populatedName);

			PopulatedType populatedType = new PopulatedType(names);
			return populatedType;
		}

		private NameInfo ConvertTypeToNameInfo(Type type)
		{
			List<ClassInfo> parents = new List<ClassInfo>();
			if (type.DeclaringType != null)
			{
				NameInfo container = ConvertTypeToNameInfo(type.DeclaringType);
				parents.AddRange(container.Classes);
				parents.Add(new ClassInfo(container.Name, container.TypeParameters, container.Flags));
			}
			else
			{
				string[] namespacePieces = type.Namespace.Split('.');
				parents.AddRange(namespacePieces.Select(n => new ClassInfo(n, null, 0)));
			}

			string name = type.Name;

			int backtickIndex;
			if ((backtickIndex = name.IndexOf('`')) >= 0)
				name = name.Substring(0, backtickIndex);

			List<string> typeParameters = null;

			if (type.IsGenericTypeDefinition)
			{
				Type[] genericArgs = type.GetGenericArguments();
				if (type.DeclaringType != null)
				{
					// The CLR will give an unexpected result for a *nested* generic
					// class:  The generic arguments above will also include the
					// generic arguments of any parent generic class.  Consider this:
					//
					//   class Foo<T, S> {
					//       class Bar<M, N> {
					//       }
					//   }
					//
					// The generic argument names for Bar are ["M", "N"], but
					// GetGenericArguments returns ["T", "S", "M", "N"], which isn't
					// strictly wrong, but isn't strictly right either.  So if there's
					// a declaring type, we get its generic argument names and remove
					// them from *our* argument names so we get the correct final set
					// of names for this type.
					HashSet<string> parentGenericArgNames = type.DeclaringType
						.GetGenericArguments()
						.Select(t => t.Name)
						.ToHashSet();
					genericArgs = genericArgs
						.Where(a => !parentGenericArgNames.Contains(a.Name))
						.ToArray();
				}
				typeParameters = genericArgs.Select(a => a.Name).ToList();
			}

			NameFlags nameFlags = 0;
			if (type.IsPublic)
				nameFlags |= NameFlags.Public;
			else
				nameFlags |= NameFlags.Internal;

			if (type.IsSealed && type.IsAbstract)
				nameFlags |= NameFlags.Static;
			else
			{
				if (type.IsSealed)
					nameFlags |= NameFlags.Sealed;
				if (type.IsAbstract)
					nameFlags |= NameFlags.Abstract;
			}

			return new NameInfo(parents, name, typeParameters, null, nameFlags);
		}
	}
}
