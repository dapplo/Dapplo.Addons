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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;

namespace Dapplo.Addons
{
    /// <summary>
    /// The interface for the ApplicationBootstrapper
    /// </summary>
    public interface IApplicationBootstrapper : IDisposable
    {
        /// <summary>
        /// Provides access to the builder, as long as it can be modified.
        /// </summary>
        ContainerBuilder Builder { get; }

        /// <summary>
        /// Provides the Autofac container
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        /// Signals when the container is created
        /// </summary>
        Action<IContainer> OnContainerCreated { get; set; }

        /// <summary>
        /// Provides the Autofac primary lifetime scope
        /// </summary>
        ILifetimeScope Scope { get; }

        /// <summary>
        /// The name of the application
        /// </summary>
        string ApplicationName { get; }

        /// <summary>
        /// Log all autofac activations
        /// </summary>
        bool EnableActivationLogging { get; set; }

        /// <summary>
        /// An IEnumerable with the loaded assemblies, but filtered to the ones not from the .NET Framework (where possible) 
        /// </summary>
        IEnumerable<Assembly> LoadedAssemblies { get; }

        /// <summary>
        ///     Returns if the Mutex is locked, in other words if the Bootstrapper can continue
        ///     This also returns true if no mutex is used
        /// </summary>
        bool IsAlreadyRunning { get; }

        /// <summary>
        /// Add the disposable to a list, everything in there is disposed when the bootstrapper is disposed.
        /// </summary>
        /// <param name="disposable">IDisposable</param>
        IApplicationBootstrapper RegisterForDisposal(IDisposable disposable);

        /// <summary>
        /// Add an additional scan directory
        /// </summary>
        /// <param name="scanDirectory">string</param>
        IApplicationBootstrapper AddScanDirectory(string scanDirectory);

        /// <summary>
        /// Add additional scan directories
        /// </summary>
        /// <param name="scanDirectories">IEnumerable</param>
        IApplicationBootstrapper AddScanDirectories(IEnumerable<string> scanDirectories);

        /// <summary>
        /// Find a certain assembly in the available scan directories and load this
        /// </summary>
        /// <param name="pattern">string with the pattern for the files</param>
        /// <param name="extensions">IEnumerable with the extensions to process, default is .dll</param>
        /// <param name="allowEmbedded">bool which specifies if the </param>
        IApplicationBootstrapper FindAndLoadAssemblies(string pattern, IEnumerable<string> extensions = null, bool allowEmbedded = true);

        /// <summary>
        /// Load the specified assembly files
        /// </summary>
        /// <param name="assemblyFiles">string array with assembly files</param>
        IApplicationBootstrapper LoadAssemblies(params string[] assemblyFiles);

        /// <summary>
        /// Load the specified assembly files
        /// </summary>
        /// <param name="assemblyFiles">IEnumerable of string</param>
        IApplicationBootstrapper LoadAssemblies(IEnumerable<string> assemblyFiles);

        /// <summary>
        /// Configure the Bootstrapper
        /// </summary>
        IApplicationBootstrapper Configure();

        /// <summary>
        /// Initialize the bootstrapper
        /// </summary>
        Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Start the IStartupModules
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        Task StartupAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Shutdown the IShutdownModules
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        Task ShutdownAsync(CancellationToken cancellationToken = default);
    }
}