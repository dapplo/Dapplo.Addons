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
using System.ComponentModel.Composition;

namespace Dapplo.Addons
{
	/// <summary>
	/// This is the Module attribute which can be used to specify type-safe meta-data
	/// Currently there are none in here, but it was made available so it's possible to add them at a later time
	/// In general it is bad to import via a specific type, always try to use contract interfaces.
	/// As the IModule is pretty much only a marker interface, it is not very usefull and this is why the attribute is abstract
	/// </summary>
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public abstract class ModuleAttribute : ExportAttribute
	{
		/// <summary>
		/// Constructor with a contractname, and a type
		/// </summary>
		public ModuleAttribute(string contractname, Type type) : base(contractname, type)
		{
		}

		/// <summary>
		/// Constructor with the type
		/// </summary>
		/// <param name="type"></param>
		public ModuleAttribute(Type type) : base(type)
		{
		}
	}
}
