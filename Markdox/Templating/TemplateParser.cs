using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Markdox.Extensions;

namespace Markdox.Templating
{
	/// <summary>
	/// This class knows how to read a .template file and transform it
	/// into C# source code.
	/// 
	/// Template files are mostly plain text, with two differences.  First, anything
	/// between {...} is a value that will be .ToString()ed and inserted into the
	/// output at that point.  Second, any like starting with % (possibly with initial
	/// whitespace) is verbatim C# code.  Everything else is simply emitted to an
	/// output StringBuilder, as given.  You may escape any of { or } or % by doubling
	/// it.
	/// 
	/// The output of this will be the *body* of a C# method, a series of statements
	/// that can then be compiled, but without any method signature.
	/// </summary>
	public class TemplateParser
	{
		private static readonly Regex _includeRegex = new Regex("^\\s*include\\s*\\\"([^\\\"]*)\"\\s*$");

		public static string Parse(string template, string filename,
			Func<string, string> includeLoader, int indent = 0)
		{
			List<Token> tokens = TemplateLexer.Tokenize(template);
			if (!tokens.Any())
				return string.Empty;

			StringBuilder stringBuilder = new StringBuilder();

			bool isFirstOnLine = true;
			string indentString = new string('\t', indent);

			filename = filename.AddCSlashes();

			stringBuilder.Append($"{indentString}#line default\r\n");

			for (int i = 0; i < tokens.Count; i++)
			{
				Token token = tokens[i];

				switch (token.Kind)
				{
					case TokenKind.Error:
						stringBuilder.Append($"{indentString}#line {token.Line} \"{filename}\"\r\n");
						stringBuilder.Append(indentString);
						stringBuilder.Append("#error\r\n");
						isFirstOnLine = false;
						break;

					case TokenKind.None:
						break;

					case TokenKind.Newline:
						stringBuilder.Append($"{indentString}#line hidden\r\n");
						stringBuilder.Append(indentString);
						stringBuilder.Append("_output.Append(\"");
						token.Text.AddCSlashesTo(stringBuilder);
						stringBuilder.Append("\");\r\n");
						stringBuilder.Append($"{indentString}#line default\r\n");
						isFirstOnLine = true;
						break;

					case TokenKind.PlainText:
						stringBuilder.Append($"{indentString}#line {token.Line} \"{filename}\"\r\n");
						stringBuilder.Append(indentString);
						string text = isFirstOnLine
							? token.Text.TrimStart()
							: token.Text;
						stringBuilder.Append("_output.Append(\"");
						text.AddCSlashesTo(stringBuilder);
						stringBuilder.Append("\");\r\n");
						isFirstOnLine = false;
						break;

					case TokenKind.Expression:
						stringBuilder.Append($"{indentString}#line hidden\r\n");
						stringBuilder.Append(indentString);
						stringBuilder.Append("{\r\n");
						stringBuilder.Append($"{indentString}\t#line {token.Line} \"{filename}\"\r\n");
						stringBuilder.Append(indentString);
						stringBuilder.Append("\tobject value = (");
						stringBuilder.Append(token.Text);
						stringBuilder.Append(");\r\n");
						stringBuilder.Append($"{indentString}\t#line hidden\r\n");
						stringBuilder.Append(indentString);
						stringBuilder.Append("\t_output.Append(value);\r\n");
						stringBuilder.Append(indentString);
						stringBuilder.Append("}\r\n");
						stringBuilder.Append($"{indentString}#line default\r\n");
						isFirstOnLine = false;
						break;

					case TokenKind.Statement:
						Match match = _includeRegex.Match(token.Text);
						if (match.Success)
						{
							string includeName = match.Groups[1].Value;
							string includeText = includeLoader(includeName);
							string includeCode = Parse(includeText, includeName, includeLoader, indent + 1);
							stringBuilder.Append(indentString);
							stringBuilder.Append("{{\r\n");
							stringBuilder.Append(includeCode);
							stringBuilder.Append(indentString);
							stringBuilder.Append("}};\r\n");
						}
						else
						{
							stringBuilder.Append($"{indentString}#line {token.Line} \"{filename}\"\r\n");

							int newIndent = indent;
							foreach (char ch in token.Text)
							{
								if (ch == '{') newIndent++;
								else if (ch == '}') newIndent--;
							}

							if (newIndent < indent)
								indentString = new string('\t', indent = newIndent);

							stringBuilder.Append(indentString);
							stringBuilder.Append(token.Text);
							stringBuilder.Append("\r\n");

							if (newIndent > indent)
								indentString = new string('\t', indent = newIndent);

							if (i < tokens.Count - 1 && tokens[i + 1].Kind == TokenKind.Newline)
								i++;
							isFirstOnLine = true;
						}
						break;
				}
			}

			stringBuilder.Append($"{indentString}#line {tokens.Last().Line} \"{filename}\"\r\n");
			stringBuilder.Append($"{indentString}#line hidden\r\n");

			return stringBuilder.ToString();
		}
	}
}
