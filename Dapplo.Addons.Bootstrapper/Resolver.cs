using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Dapplo.Addons.Bootstrapper
{
	/// <summary>
	/// This is a static resolver and loader for resources and assemblies
	/// </summary>
	public static class Resolver
	{
		private static readonly ISet<string> AppDomainRegistrations = new HashSet<string>();
		private static readonly IDictionary<string, Assembly> Assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// IEnumerable with all cached assemblies
		/// </summary>
		public static IEnumerable<Assembly> AssemblyCache => Assemblies.Values;

		/// <summary>
		///     A collection of all directories where the resolver will look to resolve resources
		/// </summary>
		public static ISet<string> ResolveDirectories { get; } = new HashSet<string>();

		#region AssemblyResolve
		/// <summary>
		/// Register the AssemblyResolve event for the specified AppDomain
		/// This can be called multiple times, it detect this.
		/// </summary>
		public static void RegisterAssemblyResolve(this AppDomain appDomain)
		{
			lock (AppDomainRegistrations)
			{
				if (!AppDomainRegistrations.Contains(appDomain.FriendlyName))
				{
					AppDomainRegistrations.Add(appDomain.FriendlyName);
					appDomain.AssemblyResolve += ResolveEventHandler;
				}
			}
		}

		/// <summary>
		/// Unegister the AssemblyResolve event for the specified AppDomain
		/// This can be called multiple times, it detect this.
		/// </summary>
		public static void UnegisterAssemblyResolve(this AppDomain appDomain)
		{
			lock (AppDomainRegistrations)
			{
				if (AppDomainRegistrations.Contains(appDomain.FriendlyName))
				{
					AppDomainRegistrations.Remove(appDomain.FriendlyName);
					appDomain.AssemblyResolve -= ResolveEventHandler;
				}
			}
		}


		/// <summary>
		///     A resolver which takes care of loading DLL's which are referenced from AddOns but not found
		/// </summary>
		/// <param name="sender">object</param>
		/// <param name="resolveEventArgs">ResolveEventArgs</param>
		/// <returns>Assembly</returns>
		private static Assembly ResolveEventHandler(object sender, ResolveEventArgs resolveEventArgs)
		{
			var assemblyName = new AssemblyName(resolveEventArgs.Name);

			return FindAssembly(assemblyName.Name);
		}
		#endregion

		#region Assembly loading

		/// <summary>
		/// Simple method to load an assembly from a file path (or returned a cached version).
		/// If it was loaded new, it will be added to the cache
		/// </summary>
		/// <param name="filepath">string with the path to the file</param>
		/// <returns>Assembly</returns>
		public static Assembly LoadAssemblyFromFile(string filepath)
		{
			var assembly = Assemblies.Values.FirstOrDefault(x => x.Location == filepath);
			if (assembly == null)
			{
				assembly = Assembly.LoadFile(filepath);
				// add the assembly to the cache
				Assemblies[assembly.GetName().Name] = assembly;
			}
			return assembly;
		}

		/// <summary>
		/// Load the specified assembly from a manifest resource or from the file system
		/// </summary>
		/// <param name="assemblyName">string from AssemblyName.Name, do not specify an extension</param>
		/// <returns>Assembly or null</returns>
		public static Assembly FindAssembly(string assemblyName)
		{
			Assembly assembly;
			// Try the cache
			if (Assemblies.TryGetValue(assemblyName, out assembly))
			{
				return assembly;
			}

			// Check manifest of know assemblies (embedded files)
			var dllName = $"{assemblyName}.dll";
			try
			{
				using (var memoryStream = GetStreamForManifestFile(dllName))
				{
					if (memoryStream != null)
					{
						// Load the assembly
						assembly = Assembly.Load(memoryStream.ToArray());
						// ass the assembly to the cache
						Assemblies[assemblyName] = assembly;
						return assembly;
					}
				}
			}
			catch (Exception ex)
			{
				// don't log with other libraries as this might cause issues / recurse resolving
				Trace.WriteLine($"Error loading {dllName} from manifest resources: {ex.Message}");
			}

			// check file system
			var dllPattern = $"{assemblyName}\\.dll";

			var filepath = ScanDirectories(ResolveDirectories, dllPattern).FirstOrDefault();
			if (!string.IsNullOrEmpty(filepath) && File.Exists(filepath))
			{
				try
				{
					assembly = LoadAssemblyFromFile(filepath);
					return assembly;
				}
				catch (Exception ex)
				{
					// don't log with other libraries as this might cause issues / recurse resolving
					Trace.WriteLine($"Error loading {filepath} : {ex.Message}");
				}
			}
			return null;
		}

		#endregion

		#region Find or load resources

		/// <summary>
		/// Find the filename in the assembly manifest resources
		/// </summary>
		/// <param name="filename">string with the filename to find</param>
		/// <returns>IEnumerable with a tuple of assemby and the resource name</returns>
		public static IEnumerable<Tuple<Assembly, string>> FindFileInManifestResources(string filename)
		{
			foreach (var availableAssembly in Assemblies.Values)
			{
				foreach (var resourceName in availableAssembly.GetManifestResourceNames())
				{
					if (resourceName.EndsWith(filename))
					{
						yield return new Tuple<Assembly, string>(availableAssembly, resourceName);
					}
				}
			}
		}

		/// <summary>
		/// Scan all assembly manifest resources for the supplied regex pattern
		/// </summary>
		/// <param name="regexPattern">Regex pattern to scan for</param>
		/// <returns>IEnumerable with a tuple of assemby and the resource name</returns>
		public static IEnumerable<Tuple<Assembly, string>> ScanManifestResources(string regexPattern)
		{
			foreach (var availableAssembly in Assemblies.Values)
			{
				foreach (var resourceName in availableAssembly.GetManifestResourceNames())
				{
					if (Regex.IsMatch(resourceName, regexPattern))
					{
						yield return new Tuple<Assembly, string>(availableAssembly, resourceName);
					}
				}
			}
		}

		/// <summary>
		/// Scan the supplied directories for files which match the supplied regex pattern
		/// </summary>
		/// <param name="directories">IEnumerable with directories to scan in</param>
		/// <param name="pattern">Regex pattern to scan for</param>
		/// <param name="isRegexpPattern">true if the pattern is a regex pattern</param>
		/// <param name="recurse">true means in subdirectories</param>
		/// <returns>IEnumerable with a tuple of assemby (null if file-system) and the resource name</returns>
		public static IEnumerable<string> ScanDirectories(IEnumerable<string> directories, string pattern, bool isRegexpPattern = true, bool recurse = true)
		{
			Regex regexSearchPattern = isRegexpPattern ? new Regex(pattern, RegexOptions.IgnoreCase) : null;
			string filePattern = isRegexpPattern ? "*" : pattern;
			foreach (var directory in directories)
			{
				var files = Directory.EnumerateFiles(directory, filePattern, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
					.Where(filepath =>
					{
						if (regexSearchPattern == null)
						{
							return true;
						}
						var file = Path.GetFileName(filepath);
						return !string.IsNullOrEmpty(file) && regexSearchPattern.IsMatch(file);
					});
				foreach (var file in files)
				{
					yield return file;
				}
			}
		}

		/// <summary>
		/// Scan all assembly manifest resources and ResolveDirectories for files which match the supplied regex pattern
		/// </summary>
		/// <param name="regexPattern">Regex pattern to scan for</param>
		/// <returns>IEnumerable with a tuple of assemby (null if file-system) and the filepath</returns>
		public static IEnumerable<Tuple<Assembly, string>> ScanAll(string regexPattern)
		{
			return ScanDirectories(ResolveDirectories, regexPattern).Select(file => new Tuple<Assembly, string>(null, file)).Union(ScanManifestResources(regexPattern));
		}

		/// <summary>
		/// Get the MemoryStream for the first found matching manifest resource
		/// </summary>
		/// <param name="filename">Only the filename, the first matching manifest resource will be used</param>
		/// <returns>MemoryStream</returns>
		public static MemoryStream GetStreamForManifestFile(string filename)
		{
			foreach (var manifestResource in FindFileInManifestResources(filename))
			{
				var memoryStream = GetManifestStream(manifestResource.Item1, manifestResource.Item2);
				if (memoryStream != null)
				{
					return memoryStream;
				}
			}
			return null;
		}

		/// <summary>
		/// Load the specified manifest resource in a memory stream
		/// </summary>
		/// <param name="assembly">Assembly to use for the resource loading</param>
		/// <param name="name">name of the resource to load</param>
		/// <returns>MemoryStream</returns>
		public static MemoryStream GetManifestStream(Assembly assembly, string name)
		{
			using (var manifestResourceStream = assembly.GetManifestResourceStream(name))
			{
				if (manifestResourceStream != null)
				{
					var memoryStream = new MemoryStream();
					manifestResourceStream.CopyTo(memoryStream);
					return memoryStream;
				}
			}
			return null;
		}

		#endregion


		#region Utils


		/// <summary>
		/// For the given directory this will return possible location.
		/// It might be that multiple are returned, also normalization is made
		/// </summary>
		/// <param name="directory">A absolute or relative directory</param>
		/// <param name="allowCurrentDirectory">true to allow relative to current working directory</param>
		/// <returns>IEnumerable with possible directories</returns>
		public static IEnumerable<string> DirectoriesFor(string directory, bool allowCurrentDirectory = true)
		{
			ISet<string> directories = new HashSet<string>();
			try
			{
				if (allowCurrentDirectory)
				{
					var normalizedDirectory = Path.GetFullPath(new Uri(directory).LocalPath)
						.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
						.ToUpperInvariant();
					if (Directory.Exists(normalizedDirectory))
					{
						directories.Add(normalizedDirectory);
					}
				}
			}
			catch
			{
				// Do nothing
			}

			if (Path.IsPathRooted(directory))
			{
				if (Directory.Exists(directory))
				{
					directories.Add(directory);
				}
			}

			// Relative to the assembly location
			try
			{
				var assemblyLocation = Assembly.GetExecutingAssembly().Location;
				if (!string.IsNullOrEmpty(assemblyLocation) && File.Exists(assemblyLocation))
				{
					var exeDirectory = Path.GetDirectoryName(assemblyLocation);
					if (!string.IsNullOrEmpty(exeDirectory) && exeDirectory != Environment.CurrentDirectory)
					{
						var relativeToExe = Path.Combine(exeDirectory, directory);
						if (Directory.Exists(relativeToExe))
						{
							directories.Add(relativeToExe);
						}
					}
				}
			}
			catch
			{
				// Do nothing
			}
			// Relative to the current working directory

			try
			{
				if (allowCurrentDirectory)
				{
					var relativetoCurrent = Path.Combine(Environment.CurrentDirectory, directory);

					if (Directory.Exists(relativetoCurrent))
					{
						directories.Add(relativetoCurrent);
					}
				}
			}
			catch
			{
				// Do nothing
			}
			return directories.OrderBy(x => x);
		}

		#endregion
	}
}
