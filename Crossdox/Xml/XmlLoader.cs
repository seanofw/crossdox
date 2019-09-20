using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Crossdox.DocTypes;
using Crossdox.Extensions;

namespace Crossdox.Xml
{
	public class XmlLoader
	{
		private Dictionary<string, TypeDoc> _types;
		private Dictionary<string, MethodDoc> _methods;
		private Dictionary<string, FieldDoc> _fields;
		private Dictionary<string, PropertyDoc> _properties;
		private Dictionary<string, EventDoc> _events;

		public TypeCollection Load(Assembly assembly)
		{
			string assemblyPath = assembly.Location;
			string assemblyDir = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
			string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
			string xmlPath = Path.Combine(assemblyDir, assemblyName + ".xml");

			return Load(xmlPath);
		}

		public TypeCollection Load(string xmlPath)
		{
			XDocument document;

			using (FileStream fileStream = new FileStream(xmlPath, FileMode.Open))
			{
				using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8))
				{
					string text = streamReader.ReadToEnd();
					document = XDocument.Parse(text);
				}
			}

			return Parse(document);
		}

		public TypeCollection Parse(XDocument document)
		{
			XElement docElement, membersElement;

			if ((docElement = document.Element("doc")) == null
				|| (membersElement = docElement.Element("members")) == null)
				return null;

			_types = new Dictionary<string, TypeDoc>();
			_methods = new Dictionary<string, MethodDoc>();
			_fields = new Dictionary<string, FieldDoc>();
			_properties = new Dictionary<string, PropertyDoc>();
			_events = new Dictionary<string, EventDoc>();

			foreach (XElement memberElement in membersElement.Elements())
			{
				if (memberElement.Name != "member") continue;
				ParseMember(memberElement);
			}

			return ConnectTheDots();
		}

		#region XML Parsing

		private void ParseMember(XElement element)
		{
			string name = element.AttributeOrDefault("name")?.Trim();
			if (string.IsNullOrEmpty(name) || name.Length < 2 || name[1] != ':') return;

			char kind = name[0];
			NameInfo nameInfo = new NameParser().Parse(name.Substring(2), kind == 'M');

			switch (kind)
			{
				case 'T':
					TypeDoc typeDoc = new TypeDoc(nameInfo,
						ParseMeta(element),
						typeParameters: ParseTypeParameters(element));
					_types.Add(typeDoc.Name, typeDoc);
					break;

				case 'M':
					MethodDoc methodDoc = new MethodDoc(nameInfo,
						ParseMeta(element),
						ParseTypeParameters(element),
						ParseParameters(element),
						ParseExceptions(element));
					_methods.Add(methodDoc.Name, methodDoc);
					break;

				case 'F':
					FieldDoc fieldDoc = new FieldDoc(nameInfo,
						ParseMeta(element));
					_fields.Add(fieldDoc.Name, fieldDoc);
					break;

				case 'P':
					PropertyDoc propertyDoc = new PropertyDoc(nameInfo,
						ParseMeta(element),
						ParseExceptions(element));
					_properties.Add(propertyDoc.Name, propertyDoc);
					break;

				case 'E':
					EventDoc eventDoc = new EventDoc(nameInfo,
						ParseMeta(element),
						ParseExceptions(element));
					_events.Add(eventDoc.Name, eventDoc);
					break;
			}
		}

		private MetaDoc ParseMeta(XElement element)
		{
			string summary = element.Element("summary")?.GetInnerXml().Trim();
			string remarks = element.Element("remarks")?.GetInnerXml().Trim();
			string example = element.Element("example")?.GetInnerXml().Trim();
			string see = element.Element("see")?.AttributeOrDefault("cref")?.Trim();
			string seeAlso = element.Element("seeAlso")?.AttributeOrDefault("cref")?.Trim();

			return new MetaDoc(summary, remarks, example, see, seeAlso);
		}

		private IEnumerable<ParameterDoc> ParseParameters(XElement element)
		{
			List<ParameterDoc> list = new List<ParameterDoc>();

			foreach (XElement paramElement in element.Elements("param"))
			{
				string name = paramElement.AttributeOrDefault("name")?.Trim();
				string description = paramElement.GetInnerXml().Trim();

				ParameterDoc parameterDoc = new ParameterDoc(name, description: description);
				list.Add(parameterDoc);
			}

			return list;
		}

		private IEnumerable<TypeParameterDoc> ParseTypeParameters(XElement element)
		{
			List<TypeParameterDoc> list = new List<TypeParameterDoc>();

			foreach (XElement typeParamElement in element.Elements("typeparam"))
			{
				string name = typeParamElement.AttributeOrDefault("name")?.Trim();
				string description = typeParamElement.GetInnerXml().Trim();

				TypeParameterDoc typeParameterDoc = new TypeParameterDoc(name, description);
				list.Add(typeParameterDoc);
			}

			return list;
		}

		private IEnumerable<ExceptionDoc> ParseExceptions(XElement element)
		{
			List<ExceptionDoc> exceptions = new List<ExceptionDoc>();

			foreach (XElement exceptionElement in element.Elements("exception"))
			{
				string cref = exceptionElement.AttributeOrDefault("cref")?.Trim();
				if (!cref.StartsWith("T:"))
					continue;
				NameInfo name = new NameParser().Parse(cref.Substring(2), false);
				exceptions.Add(new ExceptionDoc(name, element.GetInnerXml().Trim()));
			}

			return exceptions;
		}

		#endregion

		#region Connecting the pieces of XML together

		private TypeCollection ConnectTheDots()
		{
			foreach (MethodDoc method in _methods.Values)
			{
				NameInfo className = method.Name.Parent;
				if (!_types.TryGetValue(className, out TypeDoc typeDoc))
				{
					typeDoc = _types[className] = new TypeDoc(className);
				}

				_types[className] = typeDoc = typeDoc.AddMethod(method.Name.NameWithParameters, method);
			}

			foreach (FieldDoc field in _fields.Values)
			{
				NameInfo className = field.Name.Parent;
				if (!_types.TryGetValue(className, out TypeDoc typeDoc))
				{
					typeDoc = _types[className] = new TypeDoc(className);
				}

				_types[className] = typeDoc = typeDoc.AddField(field.Name.NameWithParameters, field);
			}

			foreach (PropertyDoc property in _properties.Values)
			{
				NameInfo className = property.Name.Parent;
				if (!_types.TryGetValue(className, out TypeDoc typeDoc))
				{
					typeDoc = _types[className] = new TypeDoc(className);
				}

				_types[className] = typeDoc = typeDoc.AddProperty(property.Name.NameWithParameters, property);
			}

			foreach (EventDoc @event in _events.Values)
			{
				NameInfo className = @event.Name.Parent;
				if (!_types.TryGetValue(className, out TypeDoc typeDoc))
				{
					typeDoc = _types[className] = new TypeDoc(className);
				}

				_types[className] = typeDoc = typeDoc.AddEvent(@event.Name.NameWithParameters, @event);
			}

			ImmutableDictionary<NameInfo, TypeDoc> typeDictionary = ImmutableDictionary<NameInfo, TypeDoc>.Empty;
			foreach (TypeDoc type in _types.Values)
			{
				typeDictionary = typeDictionary.Add(type.Name, type);
			}

			return new TypeCollection(typeDictionary);
		}

		#endregion
	}
}
