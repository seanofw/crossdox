using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crossdox.Extensions;

namespace Crossdox.Templating
{
	public class SourceCodeGenerator
	{
		private static string Wrapper
			=> _wrapper ?? (_wrapper = Assembly.GetExecutingAssembly()
				.GetEmbeddedResourceText("Crossdox/DefaultTemplates/TemplateWrapperClass.cs")
				.StripBom());
		private static string _wrapper;

		public static string CreateFullSourceFile(string templateText, string templateName,
			IEnumerable<TemplateArg> args, Func<string, string> includeLoader)
		{
			string methodArgs = string.Join(", ", args.Select(a => a.Type.ToString() + " " + a.Name));

			string methodText = TemplateParser.Parse(templateText, templateName, includeLoader, 4);

			string translationUnit = Wrapper
				.Replace("/*%%ARGS%%*/", methodArgs)
				.Replace("/*%%BODY%%*/", methodText);

			return translationUnit;
		}
	}
}
