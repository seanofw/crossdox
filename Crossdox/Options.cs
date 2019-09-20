using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Crossdox
{
	public class Options
	{
		public IList<string> Filenames { get; }

		public OutputKind OutputKind { get; }
		public SplitKind SplitKind { get; }
		public IncludeKind IncludeKind { get; }
		public string TemplatePath { get; }
		public string OutputPath { get; }
		public bool Verbose { get; }

		public bool Success { get; }

		private Options(
			IEnumerable<string> filenames,
			OutputKind outputKind,
			SplitKind splitKind,
			IncludeKind includeKind,
			string templatePath,
			string outputPath,
			bool verbose,
			bool success
		)
		{
			Filenames = new ReadOnlyCollection<string>(filenames.ToArray());
			OutputKind = outputKind;
			SplitKind = splitKind;
			IncludeKind = includeKind;
			TemplatePath = templatePath;
			OutputPath = outputPath;
			Verbose = verbose;
			Success = success;
		}

		public static Options Parse(string[] args)
		{
			List<string> filenames = new List<string>();
			bool lastOption = false;
			OutputKind outputKind = default;
			SplitKind splitKind = default;
			IncludeKind includeKind = default;
			string templatePath = null;
			string outputPath = null;
			bool verbose = false;
			bool success = false;

			for (int i = 0; i < args.Length; i++)
			{
				if (args[i][0] == '-' && !lastOption)
				{
					if (args[i] == "--")
					{
						lastOption = true;
						continue;
					}
					switch (args[i].Length > 1 ? args[i][1] : '-')
					{
						case '-':
							switch (args[i].Substring(2))
							{
								case "html":
									outputKind = OutputKind.Html;
									break;
								case "internal":
									includeKind |= IncludeKind.Internal;
									break;
								case "md":
								case "markdown":
									outputKind = OutputKind.Markdown;
									break;
								case "output":
									if (i >= args.Length)
									{
										Console.Error.WriteLine($"Missing pathname after {args[i]}");
										Environment.Exit(-1);
										break;
									}
									outputPath = args[++i];
									break;
								case "private":
									includeKind |= IncludeKind.Private;
									break;
								case "protected":
									includeKind |= IncludeKind.Protected;
									break;
								case "public":
									includeKind |= IncludeKind.Public;
									break;
								case "split":
								case "split-types":
									splitKind = SplitKind.SplitByNamespaceAndType;
									break;
								case "split-namespace":
									splitKind = SplitKind.SplitByNamespace;
									break;
								case "template":
									if (i >= args.Length)
									{
										Console.Error.WriteLine($"Missing pathname after {args[i]}");
										Environment.Exit(-1);
										break;
									}
									templatePath = args[++i];
									break;
								case "verbose":
									verbose = true;
									break;

								default:
									Console.Error.WriteLine($"Unknown option: {args[i]}");
									Environment.Exit(-1);
									break;
							}
							break;

						case 'm':
							outputKind = OutputKind.Markdown;
							break;

						case 's':
							splitKind = SplitKind.SplitByNamespaceAndType;
							break;

						case 't':
							if (args[i].Length > 2)
								templatePath = args[i].Substring(2);
							else
							{
								if (i >= args.Length)
								{
									Console.Error.WriteLine($"Missing pathname after {args[i]}");
									Environment.Exit(-1);
									break;
								}
								templatePath = args[++i];
							}
							break;

						case 'o':
							if (args[i].Length > 2)
								outputPath = args[i].Substring(2);
							else
							{
								if (i >= args.Length)
								{
									Console.Error.WriteLine($"Missing pathname after {args[i]}");
									Environment.Exit(-1);
									break;
								}
								outputPath = args[++i];
							}
							break;

						case 'v':
							verbose = true;
							break;

						case '?':
						case 'h':
						case 'H':
							Console.Error.WriteLine(
@"Usage: crossdox [options] assembly.dll assembly2.xml ...

Crossdox reads XML documentation output from the C# compiler and any
given compiled DLLs or EXEs, and generates cross-referenced, pretty
Markdown or HTML documentation websites from them.

Selection options:
  --public            Include public (exclude private/protected)
  --internal          Include internal (exclude private/protected)
  --protected         Include protected (exclude private)
  --private           Include private

Website options:
  --html              Output is HTML website
  -m --md --markdown  Output is Markdown (default)
  --summary           Output a summary of what does & doesn't have docs
  -t path
  --template path     Use custom HTML/Markdown template(s) from here

Output options:
  -s --split
  --split-types       Output separate files by namespace and type
  --split-namespace   Output separate files only by namespace
  -o path
  --output path       Specify output file/folder (default is '.')
  -v --verbose        Emit verbose debugging information to stderr
");
							success = false;
							break;

						default:
							Console.Error.WriteLine($"Unknown option: {args[i]}");
							success = false;
							break;
					}
				}
				else filenames.Add(args[i]);
			}

			if (includeKind == default)
				includeKind = IncludeKind.All;

			return new Options(
				filenames: filenames,
				outputKind: outputKind,
				splitKind: splitKind,
				includeKind: includeKind,
				templatePath: templatePath,
				outputPath: outputPath,
				verbose: verbose,
				success: success
			);
		}

		public void Log(string format, params object[] args)
		{
			if (!Verbose) return;

			string message = string.Format(format, args);
			Console.Error.WriteLine(message);
		}
	}
}
