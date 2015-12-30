/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
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
using System.ComponentModel.Composition;

namespace Dapplo.Addons
{
	/// <summary>
	/// This is the attribute for a IStartupAction module
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class StartupActionAttribute : ModuleAttribute, IStartupActionMetadata
	{
		public StartupActionAttribute() : base(typeof(IStartupAction))
		{

		}

		/// <summary>
		/// Use a specific contract name for the IStartupAction
		/// </summary>
		/// <param name="contractName"></param>
		public StartupActionAttribute(string contractName) : base(contractName, typeof(IStartupAction))
		{

		}


		/// <summary>
		/// Here the order of the startup action can be specified, starting with low values and ending with high.
		/// With this a cheap form of "dependency" management is made.
		/// </summary>
		public int StartupOrder
		{
			get;
			set;
		} = 1;

		/// <summary>
		/// this property describes if the StartAsync NEEDS an await, in general this is true.
		/// There are some startup actions where is makes sense to NOT await the result.
		/// These should specify a false in the annotation.
		/// </summary>
		public bool DoNotAwait
		{
			get;
			set;
		} = true;
	}
}
