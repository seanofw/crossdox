using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Markdox.RuntimeCompiling
{
	public class CompiledAssembly
	{
		public string Name { get; }
		public byte[] Dll { get; }
		public byte[] Pdb { get; }
		public bool HasErrors { get; }
		public IList<string> Errors { get; }

		public CompiledAssembly(string name, byte[] dll, byte[] pdb = null,
			bool hasErrors = false, IEnumerable<string> errors = null)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Dll = dll ?? throw new ArgumentNullException(nameof(dll));
			Pdb = pdb;

			HasErrors = hasErrors;
			Errors = new ReadOnlyCollection<string>(errors?.ToArray() ?? new string[0]);
		}

		public override string ToString()
			=> Name;

		public Assembly ToAssembly()
		{
#if NET48 || NET47 || NET46 || NET45 || NET40
			if (Pdb != null)
			{
				using (MemoryStream dllStream = new MemoryStream(Dll))
				using (MemoryStream pdbStream = new MemoryStream(Pdb))
				{
					Assembly assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(dllStream, pdbStream);
					return assembly;
				}
			}
			else
			{
				using (MemoryStream dllStream = new MemoryStream(Dll))
				{
					Assembly assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(dllStream);
					return assembly;
				}
			}
#else
			if (Pdb != null)
			{
				Assembly assembly = Assembly.Load(Dll, Pdb);
				return assembly;
			}
			else
			{
				Assembly assembly = Assembly.Load(Dll);
				return assembly;
			}
#endif
		}
	}
}
