using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Markdox.Extensions;

namespace Markdox.RuntimeCompiling
{
	public class CompiledAssemblyCache
	{
		private readonly string _folder;

		private readonly Dictionary<string, CompiledAssembly> _templates
			= new Dictionary<string, CompiledAssembly>();

		public CompiledAssemblyCache(string folder)
		{
			_folder = Path.GetFullPath(folder);
		}

		public CompiledAssembly LoadOrCompile(Options options,
			string sourceText, string sourceFilename,
			IEnumerable<string> additionalReferences = null)
		{
			CompiledAssembly assembly;

			options.Log("Generating final source code form.");

			// Generate the final compiled text itself.
			string finalSourceText = GenerateFinalSourceText(sourceText, sourceFilename, additionalReferences);

			// We use a content-addressible cache:  The hash of the compiled
			// text is the name of the cache file.  We truncate the hash to its first
			// 10 bytes (80 bits), since we don't need cryptographic security:  We
			// just need a low probability of an accidental collision, and 80 bits
			// is plenty for that.
			string hashName = finalSourceText.SHA256().ToHexChars(false).Substring(0, 20);

			options.Log("Assembly name: {0}.dll", hashName);

			// If we already have it in memory, just use it.
			if (_templates.TryGetValue(hashName, out assembly))
			{
				options.Log("Using in-memory cached assembly.");
				return assembly;
			}

			options.Log("Searching for assembly in cache folder \"{0}\".", _folder);

			// Choose suitable output pathnames.
			string dllPath = Path.Combine(_folder, hashName + ".dll");
			string pdbPath = Path.Combine(_folder, hashName + ".pdb");

			// Try to load the cached DLL and PDB files.  If they exist,
			// we'll use them:  They have the right hash, so they must be
			// the right data.
			byte[] cachedDll, cachedPdb;

			// First, try to get the DLL.  It's okay if we can't.
			try
			{
				cachedDll = File.ReadAllBytes(dllPath);
				if (cachedDll.Length == 0)
					cachedDll = null;
			}
			catch (IOException)
			{
				cachedDll = null;
			}

			if (cachedDll != null)
			{
				// Maybe load the PDB, if it exists.  If it doesn't, no big deal:
				// the user just doesn't get debugging information, but at least
				// we don't have to compile the source code again.
				try
				{
					cachedPdb = File.ReadAllBytes(pdbPath);
					if (cachedPdb.Length == 0)
						cachedPdb = null;
				}
				catch (IOException)
				{
					cachedPdb = null;
				}

				options.Log("Using on-disk cached assembly.");

				// Got at least the DLL, if not the PDB, so we're good to go.
				assembly = new CompiledAssembly(hashName, cachedDll, cachedPdb);

				// Keep a copy in memory so we don't have to do it again.
				_templates.Add(hashName, assembly);

				// And we're done.
				return assembly;
			}

			options.Log("Compiling new assembly.");

			// Don't have it, so compile it for real.  This can be slow (seconds,
			// not milliseconds or microseconds).
			assembly = CSharpCompiler.Compile(finalSourceText,
				sourceFilename, hashName, additionalReferences);

			// Don't bother caching it if we can't compile it.
			if (assembly.HasErrors)
			{
				options.Log("Compile failed with errors.");
				return assembly;
			}

			options.Log("Writing new assembly to cache folder \"{0}\".", _folder);

			// Try to write the compiled assembly to disk.  If we can't,
			// the cache is just slower across runs, but it's not really
			// an error.
			try
			{
				Directory.CreateDirectory(_folder);
			}
			catch (Exception e)
			{
				options.Log("Cannot ensure \"{0}\" exists: {1}", _folder, e.Message);
			}

			try
			{
				File.WriteAllBytes(dllPath, assembly.Dll);
			}
			catch (Exception e)
			{
				options.Log("Cannot write to \"{0}\": {1}", dllPath, e.Message);
			}

			try
			{
				File.WriteAllBytes(pdbPath, assembly.Pdb);
			}
			catch (Exception e)
			{
				options.Log("Cannot write to \"{0}\": {1}", pdbPath, e.Message);
			}

			return assembly;
		}

		/// <summary>
		/// Add additional text comments so that the resulting source code is content-addressible
		/// to itself (i.e., a hash of it should always point at the same source code, so we
		/// add in the name of the source file, the assemblies the code references, and so on).
		/// </summary>
		private static string GenerateFinalSourceText(string sourceText,
			string sourceFilename, IEnumerable<string> additionalReferences)
		{
			string[] additionalReferenceArray = additionalReferences?.ToArray() ?? new string[0];

			sourceText = sourceText.StripBom();

			StringBuilder stringBuilder = new StringBuilder();

			stringBuilder.Append("//! name: ");
			stringBuilder.Append(sourceFilename);
			stringBuilder.Append("\r\n");

			// Including the current date in the source text keeps the cache from
			// getting too stale.
			DateTime dateTime = DateTime.Now;
			stringBuilder.Append("//! date: ");
			stringBuilder.Append(dateTime.ToShortDateString());
			stringBuilder.Append("\r\n");

			foreach (string additionalReference in additionalReferenceArray)
			{
				stringBuilder.Append("//! reference: ");
				stringBuilder.Append(additionalReference);
				stringBuilder.Append("\r\n");
			}

			stringBuilder.Append("\r\n");

			stringBuilder.Append(sourceText);

			string finalSourceText = stringBuilder.ToString();
			return finalSourceText;
		}
	}
}
