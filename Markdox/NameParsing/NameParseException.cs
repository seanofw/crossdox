﻿using System;
using System.Runtime.Serialization;

namespace Markdox.Xml
{
	public class NameParseException : Exception
	{
		public NameParseException(string message)
			: base(message)
		{
		}

		public NameParseException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}