// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2019 Dapplo
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

using System;

namespace Dapplo.Addons.TestAddon
{
    public class StartupExceptionThrowingAddon : IStartup
    {
        /// <summary>
        ///     This imports a bool which is set in the test case and specifies if this addon needs to throw a startup exception
        /// </summary>
        public bool ThrowStartupException { get; set; }

        public void Startup()
        {
            if (ThrowStartupException)
            {
                throw new NotSupportedException("I was ordered to!!!");
            }
        }
    }
}