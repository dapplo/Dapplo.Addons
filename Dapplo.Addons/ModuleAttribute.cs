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
using System.ComponentModel.Composition;

#endregion

namespace Dapplo.Addons
{
	/// <summary>
	///     This is the Module attribute which can be used to specify type-safe meta-data
	///     Currently there are none in here, but it was made available so it's possible to add them at a later time
	///     In general it is bad to import via a specific type, always try to use contract interfaces.
	///     As the IModule is pretty much only a marker interface, it is not very usefull and this is why the attribute is
	///     abstract
	/// </summary>
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public abstract class ModuleAttribute : ExportAttribute
	{
		/// <summary>
		///     Constructor with a contractname, and a type
		/// </summary>
		protected ModuleAttribute(string contractname, Type type) : base(contractname, type)
		{
		}

		/// <summary>
		///     Constructor with the type
		/// </summary>
		/// <param name="type"></param>
		protected ModuleAttribute(Type type) : base(type)
		{
		}
	}
}