using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Markdox.Extensions
{
	public static class AssemblyExtensions
	{
		public static byte[] GetEmbeddedResource(this Assembly assembly, string name)
		{
			try
			{
				using (Stream stream = assembly.GetManifestResourceStream(name.Replace('\\', '.').Replace('/', '.')))
				{
					if (stream == null)
						throw new ResourceException($"Embedded resource \"{name}\" cannot be loaded: No resource by this name exists.");

					byte[] buffer = new byte[stream.Length];
					stream.Read(buffer, 0, (int)stream.Length);
					return buffer;
				}
			}
			catch (Exception e)
			{
				if (!(e is ResourceException))
					throw new ResourceException($"Embedded resource \"{name}\" cannot be loaded: {e.Message}", e);
				throw;
			}
		}

		public static string GetEmbeddedResourceText(this Assembly assembly, string name)
		{
			byte[] bytes = assembly.GetEmbeddedResource(name);
			return Encoding.UTF8.GetString(bytes);
		}
	}
}
