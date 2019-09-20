using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Crossdox;
using Crossdox.DocTypes;
using Crossdox.Extensions;

namespace TemplateScript
{
	public class CompiledTemplate
	{
		public void Output(object value)
			=> _output.Append(value);

		public void Output(string text)
			=> _output.Append(text);

		private StringBuilder _output;

		public string Run(/*%%ARGS%%*/)
		{
			_output = new StringBuilder();

{{
/*%%BODY%%*/}};return _output.ToString();}}}