#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
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
    ///     This is the attribute for a IShutdownModule module
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ShutdownActionAttribute : ModuleAttribute, IShutdownMetadata
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        public ShutdownActionAttribute() : base(typeof(IShutdownModule))
        {
        }

        /// <summary>
        ///     Constructor with a contract name
        /// </summary>
        /// <param name="contractName"></param>
        public ShutdownActionAttribute(string contractName) : base(contractName, typeof(IShutdownModule))
        {
        }

        /// <summary>
        ///     Order for the shutdowns to be called
        /// </summary>
        public int ShutdownOrder { get; set; }
    }
}