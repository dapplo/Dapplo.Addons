﻿/*
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

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace Dapplo.Addons.Implementation
{
	/// <summary>
	/// A bootstrapper for making it possible to load Addons to Dapplo applications.
	/// This uses MEF for loading and managing the Addons.
	/// </summary>
	public abstract class CompositionBootstrapper
	{
		protected AggregateCatalog AggregateCatalog
		{
			get;
			private set;
		}

		public CompositionContainer Container
		{
			get;
			private set;
		}

		protected CompositionBootstrapper()
		{
			AggregateCatalog = new AggregateCatalog();
		}

		/// <summary>
		/// Override this method to extend what is loaded into the Catalog
		/// </summary>
		protected virtual void ConfigureAggregateCatalog()
		{
		}

		protected virtual void ConfigureContainer()
		{
			// Export the container itself
			Container.ComposeExportedValue(Container);
		}

		public void Run()
		{
			ConfigureAggregateCatalog();
			Container = new CompositionContainer(AggregateCatalog, CompositionOptions.DisableSilentRejection);
			ConfigureContainer();
			Container.ComposeParts();
		}
	}
}
