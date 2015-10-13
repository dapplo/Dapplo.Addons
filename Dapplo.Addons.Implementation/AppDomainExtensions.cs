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

using System;

namespace Dapplo.Addons.Implementation
{
	public static class AppDomainExtensions
	{
		/// <summary>
		/// Attach a nuget Assembly resolved to the AppDomain
		/// This could be called multiple times, for multiple package sources
		/// </summary>
		/// <param name="currentAppDomain">AppDomain to attach the nuget resolver to</param>
		/// <param name="localPackageSource">Local path for storing the downloaded packages</param>
		/// <param name="remotePackageSource">Remote package source, either NuGet.org or your own package source</param>
		public static void AttachNugetResolver(this AppDomain currentAppDomain, string localPackageSource = @"nuget-repository", string remotePackageSource = "https://packages.nuget.org/api/v2")
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
