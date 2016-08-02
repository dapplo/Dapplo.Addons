#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Addons
// 
// Dapplo.Addons is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Addons is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;

#endregion

namespace Dapplo.Addons.NuGet
{
	public static class AppDomainExtensions
	{
		/// <summary>
		///     Attach a nuget Assembly resolved to the AppDomain
		///     This could be called multiple times, for multiple package sources
		/// </summary>
		/// <param name="currentAppDomain">AppDomain to attach the nuget resolver to</param>
		/// <param name="localPackageSource">Local path for storing the downloaded packages</param>
		/// <param name="remotePackageSource">Remote package source, either NuGet.org or your own package source</param>
		public static void AttachNugetResolver(this AppDomain currentAppDomain, string localPackageSource = @"nuget-repository",
			string remotePackageSource = "https://packages.nuget.org/api/v2")
		{
			var nugetResolver = new NuGetResolver
			{
				LocalPackageSource = localPackageSource,
				RemotePackageSource = remotePackageSource
			};
			currentAppDomain.AssemblyResolve += nugetResolver.NugetResolveEventHandler;
		}
	}
}