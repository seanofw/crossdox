using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Markdox.DocTypes;
using Markdox.Extensions;
using Markdox.Reflection;
using Markdox.RuntimeCompiling;
using Markdox.Templating;
using Markdox.Xml;

namespace Markdox
{
	class Program
	{
		static void Main(string[] args)
		{
			Options options = Options.Parse(args);

			if (!options.Filenames.Any())
			{
				Console.Error.WriteLine("Usage: markdox [options] assembly.dll assembly2.xml ...   (use -h for help)");
				Environment.Exit(-1);
			}

			options.Log("Starting.");

			if (!Run(options))
			{
				options.Log("Stopped with errors.");
				Environment.ExitCode = 1;
			}
			else
			{
				options.Log("Finished successfully.");
				Environment.ExitCode = 0;
			}
		}

		private static bool Run(Options options)
		{
			TypeCollection totalTypeCollection;
			try
			{
				totalTypeCollection = LoadAll(options);
			}
			catch (IOException e)
			{
				Console.WriteLine("Error loading files: " + e.Message);
				return false;
			}

			options.Log("Preparing output template C# source code.");

			string template = Assembly.GetExecutingAssembly()
				.GetEmbeddedResourceText("Markdox/DefaultTemplates/SingleDocument.template");

			string sourceCode = SourceCodeGenerator.CreateFullSourceFile(template,
				"SingleDocument.template",
				new[]
				{
					new TemplateArg(typeof(NameInfo), "RootNamespace"),
					new TemplateArg(typeof(TypeCollection), "Types")
				},
				includeName =>
				{
					string includeText = Assembly.GetExecutingAssembly()
						.GetEmbeddedResourceText("Markdox/DefaultTemplates/" + includeName);
					return includeText;
				}
			);

			options.Log("Compiling output template source code.");

			string markdoxCacheFolder = Path.Combine(Path.GetTempPath(), "markdox");
			CompiledAssemblyCache assemblyCache = new CompiledAssemblyCache(markdoxCacheFolder);
			CompiledAssembly compiledAssembly = assemblyCache.LoadOrCompile(options, sourceCode, "SingleDocument.template");
			if (compiledAssembly.HasErrors)
			{
				foreach (string error in compiledAssembly.Errors)
				{
					Console.Error.WriteLine("Error compiling script: " + error);
				}
				return false;
			}

			Assembly assembly = compiledAssembly.ToAssembly();

			options.Log("Reflecting against template assembly.");

			Type compiledTemplate = assembly.GetType("TemplateScript.CompiledTemplate");
			ConstructorInfo constructor = compiledTemplate.GetConstructor(Type.EmptyTypes);
			object compiledTemplateInstance = constructor.Invoke(new object[0]);

			MethodInfo runMethod = compiledTemplate.GetMethod("Run");
			object[] runArgs = new object[]
			{
				new NameInfo(null, "Test", null, null, default),
				totalTypeCollection,
			};

			options.Log("Invoking template assembly Run() method.");

			string result;
			try
			{
				result = (string)runMethod.Invoke(compiledTemplateInstance, runArgs);
			}
			catch (TargetInvocationException e)
			{
				if (e.InnerException != null)
				{
					Console.Error.WriteLine("Error running script: " + e.InnerException);
				}
				else
				{
					Console.Error.WriteLine("Error running script: " + e);
				}
				return false;
			}

			if (result != null)
			{
				Console.WriteLine(result);
			}

			return true;
		}

		private static TypeCollection LoadAll(Options options)
		{
			TypeCollection totalTypeCollection = new TypeCollection();

			options.Log("Loading input files.");

			foreach (string filename in options.Filenames)
			{
				if (filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
				{
					options.Log("Loading XML \"{0}\".", filename);
					TypeCollection typeCollection = new XmlLoader().Load(filename);

					totalTypeCollection = totalTypeCollection.AddTypeRange(typeCollection);
				}
				else if (filename.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
					|| filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				{
					options.Log("Loading assembly \"{0}\".", filename);
					Assembly assembly = Assembly.LoadFrom(filename);

					options.Log("Loading XML for assembly.");
					TypeCollection xmlCollection = new XmlLoader().Load(assembly);

					options.Log("Reflecting on assembly.");
					TypeCollection reflectedCollection = new TypeLoader().ReflectOnAssembly(assembly, options.IncludeKind);

					options.Log("Combining reflected data with XML.");
					TypeCollection typeCollection = reflectedCollection.MergeInDocumentation(xmlCollection);

					totalTypeCollection = totalTypeCollection.AddTypeRange(typeCollection);
				}
			}

			options.Log("All input files loaded.");

			return totalTypeCollection;
		}
	}
}
