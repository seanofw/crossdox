using System;

namespace Crossdox.Extensions
{
	public class ResourceException : Exception
	{
		internal ResourceException(string message)
			: base(message)
		{
		}

		internal ResourceException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}