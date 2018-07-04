#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.CaliburnMicro
// 
// Dapplo.CaliburnMicro is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.CaliburnMicro is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.CaliburnMicro. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;

namespace Dapplo.Addons.Bootstrapper.Services
{
    /// <summary>
    /// This is an abstract implementation of a service handler, which can be used if you want to reuse the concept.
    /// </summary>
    public abstract class AbstractServiceHandler<TService>
    {
        /// <summary>
        /// This contains all the nodes for your services
        /// </summary>
        public IDictionary<string, ServiceNode<TService>> ServiceNodes { get; }

        /// <summary>
        /// The constructor to specify the startup modules
        /// </summary>
        /// <param name="services">IEnumerable</param>
        public AbstractServiceHandler(IEnumerable<Meta<TService, ServiceAttribute>> services) => ServiceNodes = CreateServiceDictionary(services);

        /// <summary>
        /// Internal helper, used for the test cases too
        /// </summary>
        /// <param name="services">IEnumerable with Meta of IService and ServiceAttribute</param>
        /// <returns>IDictionary</returns>
        public static IDictionary<string, ServiceNode<TService>> CreateServiceDictionary(IEnumerable<Meta<TService, ServiceAttribute>> services)
        {
            var serviceNodes = services.ToDictionary(meta => meta.Metadata.Name, meta => new ServiceNode<TService>
            {
                Details = meta.Metadata,
                Service = meta.Value
            });

            // Enrich the information
            foreach (var serviceNode in serviceNodes.Values)
            {
                var serviceAttribute = serviceNode.Details;
                // check if this depends on anything
                if (string.IsNullOrEmpty(serviceAttribute.Prerequisite))
                {
                    // Doesn't have any prerequisites
                    continue;
                }

                if (!serviceNodes.TryGetValue(serviceAttribute.Name, out var thisNode))
                {
                    throw new NotSupportedException($"Coudn't find service with Name {serviceAttribute.Name}");
                }

                if (!serviceNodes.TryGetValue(serviceAttribute.Prerequisite, out var prerequisiteNode))
                {
                    if (!serviceAttribute.SkipIfPrerequisiteIsMissing)
                    {
                        throw new NotSupportedException($"Coudn't find service with Name {serviceAttribute.Prerequisite}, service {serviceAttribute.Name} depends on this.");
                    }
                    continue;
                }
                thisNode.Prerequisites.Add(prerequisiteNode);
                prerequisiteNode.Dependencies.Add(thisNode);
            }

            return serviceNodes;
        }

        /// <summary>
        /// Start the services, begin with the root nodes and than everything that depends on these, and so on
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task to await startup</returns>
        public abstract Task StartupAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop the services, begin with the nodes without dependencies and than everything that this depends on, and so on
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task to await startup</returns>
        public abstract Task ShutdownAsync(CancellationToken cancellationToken = default);
    }
}
