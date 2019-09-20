using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Markdox.RuntimeCompiling
{
	public class CSharpCompiler
	{
		private static readonly string[] _standardReferences = new[]
		{
			"System.dll",
			"System.Core.dll",
			"System.Runtime.dll",
			"System.Collections.dll",
			"System.Collections.Immutable.dll",
			"System.Linq.dll",
			"System.Text.RegularExpressions.dll",
		};

		public static CompiledAssembly Compile(string sourceText, string sourceFilename,
			string assemblyName, IEnumerable<string> additionalReferences = null)
		{
			try
			{
				bool hasErrors;
				List<string> errorMessages;

				SyntaxTree syntaxTree = ParseSourceCode(sourceText, sourceFilename);
				errorMessages = syntaxTree.GetDiagnostics()
					.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
					.Select(d => d.ToString())
					.ToList();
				if (errorMessages.Any())
					return new CompiledAssembly(assemblyName, new byte[0], new byte[0], true, errorMessages);

				MetadataReference[] references = CollectAssemblyReferences(additionalReferences, out hasErrors, out errorMessages);
				if (hasErrors)
					return new CompiledAssembly(assemblyName, new byte[0], new byte[0], true, errorMessages);

				CSharpCompilation compilation = SetupCompiler(assemblyName, syntaxTree, references);

				CompiledAssembly compiledTemplate = EmitCompiledTemplate(assemblyName, compilation);
				return compiledTemplate;
			}
			catch (Exception e)
			{
				return new CompiledAssembly(assemblyName, new byte[0], new byte[0], true,
					new[] { "Internal compiler error: " + e.Message });
			}
		}

		private static SyntaxTree ParseSourceCode(string translationUnit, string templateName)
		{
			SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
				text: translationUnit,
				options: new CSharpParseOptions(
					languageVersion: LanguageVersion.CSharp7,
					documentationMode: DocumentationMode.None,
					kind: SourceCodeKind.Regular),
				path: templateName,
				encoding: Encoding.UTF8
			);

			return syntaxTree;
		}

		private static MetadataReference[] CollectAssemblyReferences(IEnumerable<string> additionalReferences,
			out bool hasErrors, out List<string> errorMessages)
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();

			string coreLibLocation = typeof(object).GetTypeInfo().Assembly.Location;
			string coreLibPath = Path.GetDirectoryName(coreLibLocation);

			List<string> references = new List<string>();

			references.Add(coreLibLocation);
			references.AddRange(_standardReferences.Select(r => Path.Combine(coreLibPath, r)));
			if (additionalReferences != null)
				references.AddRange(additionalReferences);
			references.Add(executingAssembly.Location);

			errorMessages = new List<string>();
			hasErrors = false;
			MetadataReference[] metadataReferences = new MetadataReference[references.Count];

			for (int i = 0; i < references.Count; i++)
			{
				try
				{
					MetadataReference reference = MetadataReference.CreateFromFile(references[i]);
					metadataReferences[i] = reference;
				}
				catch (Exception e)
				{
					hasErrors = true;
					errorMessages.Add($"Could not load reference \"{references[i]}\": {e.Message}");
				}
			}

			return metadataReferences;
		}

		private static CSharpCompilation SetupCompiler(string newAssemblyName, SyntaxTree syntaxTree, MetadataReference[] references)
		{
			return CSharpCompilation.Create(
				newAssemblyName, new[] { syntaxTree }, references,
				new CSharpCompilationOptions(
					outputKind: Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary
				)
			);
		}

		private static CompiledAssembly EmitCompiledTemplate(string newAssemblyName, CSharpCompilation compilation)
		{
			using (MemoryStream dllMemoryStream = new MemoryStream())
			using (MemoryStream pdbMemoryStream = new MemoryStream())
			{
				EmitResult result = compilation.Emit(dllMemoryStream, pdbMemoryStream);

				if (result.Success)
				{
					dllMemoryStream.Seek(0, SeekOrigin.Begin);
					pdbMemoryStream.Seek(0, SeekOrigin.Begin);

					CompiledAssembly compiledTemplate = new CompiledAssembly(newAssemblyName,
						dllMemoryStream.ToArray(), pdbMemoryStream.ToArray(), false, null);

					return compiledTemplate;
				}
				else
				{
					List<string> errorMessages = result.Diagnostics
						.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
						.Select(d => d.ToString())
						.ToList();

					CompiledAssembly compiledTemplate = new CompiledAssembly(newAssemblyName,
						new byte[0], new byte[0], true, errorMessages);

					return compiledTemplate;
				}
			}
		}
	}
}
