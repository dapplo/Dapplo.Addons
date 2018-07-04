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
using Autofac.Features.Metadata;

namespace Dapplo.Addons.Services
{
    /// <summary>
    /// This can hold the servicenodes for a tree based dependency startup, shutdown or simular services
    /// </summary>
    public class ServiceNodeContainer<TService>
    {
        /// <summary>
        /// This contains all the nodes for your services
        /// </summary>
        public IReadOnlyDictionary<string, ServiceNode<TService>> ServiceNodes { get; }

        /// <summary>
        /// The constructor to specify the services
        /// </summary>
        /// <param name="services">IEnumerable</param>
        public ServiceNodeContainer(IEnumerable<Meta<TService, ServiceAttribute>> services) => ServiceNodes = CreateServiceDictionary(services);

        /// <summary>
        /// This builds a tree of servicenodes
        /// </summary>
        /// <param name="services">IEnumerable with Meta of IService and ServiceAttribute</param>
        /// <returns>IDictionary</returns>
        private static IReadOnlyDictionary<string, ServiceNode<TService>> CreateServiceDictionary(IEnumerable<Meta<TService, ServiceAttribute>> services)
        {
            var serviceNodes = new Dictionary<string, ServiceNode<TService>>();
            // Build dictionary and check some constrains
            foreach (var service in services)
            {
                var name = service.Metadata.Name ?? throw new ArgumentNullException(nameof(service.Metadata.Name), $"{service.Value.GetType()} doesn't have a name.");
                if (serviceNodes.ContainsKey(name))
                {
                    throw new NotSupportedException($"{service.Value.GetType()} uses a duplicate name: {service.Metadata.Name}");
                }
                serviceNodes.Add(name, new ServiceNode<TService>
                {
                    Details = service.Metadata,
                    Service = service.Value
                });
            }

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
    }
}
