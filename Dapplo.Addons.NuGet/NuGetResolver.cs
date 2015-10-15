/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dapplo.Addons.NuGet
{
	/// <summary>
	/// The nuget resolver can handle "ResolveEvents" by checking if the missing Assembly can be downloaded from Nuget
	/// </summary>
	internal class NuGetResolver
	{
		/// <summary>
		/// Location where the local packages will be stored
		/// </summary>
		internal string LocalPackageSource
		{
			get;
			set;
		}

		/// <summary>
		/// Remote package source where the nuget packages will be looked for an downloaded
		/// Default is "https://packages.nuget.org/api/v2" but could just as well be your own package source
		/// </summary>
		internal string RemotePackageSource
		{
			get;
			set;
		}

		/// <summary>
		/// The NuGet Resolver default construction will set some defaults
		/// </summary>
		internal NuGetResolver()
		{
			LocalPackageSource = @"nuget-repository";
			RemotePackageSource = "https://packages.nuget.org/api/v2";
		}

		/// <summary>
		/// Try to resolve the requested assembly via Nuget, locally and remote.
		/// </summary>
		internal Assembly NugetResolveEventHandler(object sender, ResolveEventArgs resolveEventArgs)
		{
			if (resolveEventArgs.Name.Contains("resources"))
			{
				return null;
			}
			var assemblyName = new AssemblyName(resolveEventArgs.Name);
			//LOG.Info("Trying to resolve {0}", assemblyName.Name);

			try
			{
				IPackageRepository remoteRepository = PackageRepositoryFactory.Default.CreateRepository(RemotePackageSource);
				// Create a package manager for managing our local repository
				IPackageManager packageManager = new PackageManager(remoteRepository, LocalPackageSource);
				IPackageRepository localRepository = packageManager.LocalRepository;
				var localPackages = localRepository.FindPackagesById(assemblyName.Name);

				IPackage locatedPackage = FindPackage(assemblyName, localPackages);
				if (locatedPackage == null)
				{
					// Search package via NuGet remote
					var remotePackages = remoteRepository.FindPackagesById(assemblyName.Name);
					locatedPackage = FindPackage(assemblyName, remotePackages);
					if (locatedPackage != null)
					{
						packageManager.InstallPackage(locatedPackage.Id, locatedPackage.Version, true, false);
					}
				}
				return ReturnAssemblyFromRepository(packageManager, assemblyName);
			}
			catch (Exception)
			{
				//LOG.Warn(ex, "Problem using NuGet find an unresolved assembly");
			}
			return null;
		}

		/// <summary>
		/// Find a package matching the supplied assemblyname in the list of packages
		/// </summary>
		private static IPackage FindPackage(AssemblyName assemblyName, IEnumerable<IPackage> packages)
		{
			IPackage optionalOther = null;
			foreach (IPackage availablePackage in packages.OrderBy(package => package.Version.ToString()))
			{
				// Find the needed version
				if (assemblyName.Version.Equals(availablePackage.Version.Version))
				{
					return availablePackage;
				}
				optionalOther = availablePackage;
			}
			return optionalOther;
		}

		/// <summary>
		/// Retrieve the assembly for the supplied package, or null
		/// </summary>
		private static Assembly ReturnAssemblyFromRepository(IPackageManager packageManager, AssemblyName assemblyName)
		{
			if (assemblyName == null)
			{
				return null;
			}
			string basePath = Path.GetFullPath(packageManager.LocalRepository.Source);
			if (Directory.Exists(basePath))
			{
				string dllPath = Directory.EnumerateFiles(basePath, assemblyName.Name + ".dll", SearchOption.AllDirectories).OrderBy(path => path).LastOrDefault();
				if (!string.IsNullOrEmpty(dllPath))
				{
					//LOG.Info("Dll found in Package {0}, installed here {1}", assemblyName.Name, dllPath);
					if (File.Exists(dllPath))
					{
						return Assembly.LoadFrom(dllPath);
						// The following doesn't work, as Fusion isn't called. See: http://blogs.msdn.com/b/suzcook/archive/2003/09/19/loadfile-vs-loadfrom.aspx
						// return Assembly.LoadFile(dllPath);
					}
				}
			}
			return null;
		}

	}
}
