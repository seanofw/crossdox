using System.Xml;
using System.Xml.Linq;

namespace Crossdox.Extensions
{
	public static class XElementExtensions
	{
		public static string AttributeOrDefault(this XElement element, string attributeName, string defaultValue = null)
			=> element.Attribute(attributeName)?.Value ?? defaultValue;

		public static string GetInnerXml(this XElement element)
		{
			XmlReader reader = element.CreateReader();
			reader.MoveToContent();

			return reader.ReadInnerXml();
		}
	}
}
