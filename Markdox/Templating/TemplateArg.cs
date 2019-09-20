using System;

namespace Markdox.Templating
{
	public struct TemplateArg
	{
		public Type Type { get; }
		public string Name { get; }

		public TemplateArg(Type type, string name)
		{
			Type = type;
			Name = name;
		}
	}

}
