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

namespace Dapplo.Addons.TestAddon
{
    [StartupAction(AwaitStart = true, StartupOrder = 1)]
    public class StartupExceptionThrowingAddon : IStartupAction
    {
        /// <summary>
        ///     This imports a bool which is set in the test case and specifies if this addon needs to throw a startup exception
        /// </summary>
        [Import(AllowDefault = true)]
        private bool ThrowStartupException { get; set; }

        public void Start()
        {
            if (ThrowStartupException)
            {
                throw new StartupException("I was ordered to!!!", new NotSupportedException());
            }
        }
    }
}