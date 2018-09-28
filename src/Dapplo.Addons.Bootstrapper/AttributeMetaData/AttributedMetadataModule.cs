using Autofac;
using Autofac.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper.AttributeMetaData
{
    /// <summary>
    /// this module will scan all registrations for metadata and associate them if found
    /// </summary>
    public class AttributedMetadataModule : Module
    {
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// Override to attach module-specific functionality to a
        /// component registration.
        /// </summary>
        /// <remarks>This method will be called for all existing <i>and future</i> component
        /// registrations - ordering is not important.</remarks>
        /// <param name="componentRegistry">The component registry.</param>
        /// <param name="registration">The registration to attach functionality to.</param>
        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException("registration");
            }
            Log.Debug().WriteLine("Processing {0}", registration.Activator.LimitType);
            foreach (var property in GetMetadata(registration.Activator.LimitType))
            {
                registration.Metadata[property.Key] = property.Value;
            }
            //base.AttachToComponentRegistration(componentRegistry, registration);
        }

        /// <summary>
        /// Given a target attribute object, returns a set of public readable properties and associated values.
        /// </summary>
        /// <param name="target">Target attribute instance to be scanned.</param>
        /// <param name="instanceType">
        /// The <see cref="Type"/> on which the <paramref name="target" /> attribute
        /// is associated. Used when the <paramref name="target" /> is an
        /// <see cref="IMetadataProvider"/>.
        /// </param>
        /// <returns>Enumerable set of property names and associated values.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="target" /> or <paramref name="instanceType"/> is <see langword="null" />.
        /// </exception>
        private static IEnumerable<KeyValuePair<string, object>> GetProperties(object target, Type instanceType)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            if (instanceType == null)
            {
                throw new ArgumentNullException("instanceType");
            }

            if (target is IMetadataProvider asProvider)
            {
                // This attribute instance decides its own properties.
                return asProvider.GetMetadata(instanceType);
            }

            return target.GetType()
                         .GetProperties()
                         .Where(propertyInfo => propertyInfo.CanRead &&
                                propertyInfo.DeclaringType != null &&
                                propertyInfo.DeclaringType.Name != typeof(Attribute).Name)
                         .Select(propertyInfo =>
                                  new KeyValuePair<string, object>(propertyInfo.Name, propertyInfo.GetValue(target, null)));
        }


        /// <summary>
        /// Given a component type, interrogates the metadata attributes to retrieve a set of property name/value metadata pairs.
        /// </summary>
        /// <param name="targetType">Type to interrogate for metdata attributes.</param>
        /// <returns>Enumerable set of property names and associated values found.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="targetType" /> is <see langword="null" />.
        /// </exception>
        private static IEnumerable<KeyValuePair<string, object>> GetMetadata(Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException("targetType");
            }
            var propertyList = new List<KeyValuePair<string, object>>();

            foreach (var attribute in targetType.GetCustomAttributes(true)
                                                .Where(p => p.GetType().GetCustomAttributes(typeof(MetadataAttributeAttribute), true).Any()))
                propertyList.AddRange(GetProperties(attribute, targetType));

            return propertyList;
        }

        /// <summary>
        /// Given a component type, interrogates the metadata attributes to retrieve
        /// a set of property name/value metadata pairs provided by a specific
        /// metadata provider.
        /// </summary>
        /// <typeparam name="TMetadataType">Metadata type to look for in the list of attributes.</typeparam>
        /// <param name="targetType">Type to interrogate.</param>
        /// <returns>Enumerable set of property names and associated values found.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="targetType" /> is <see langword="null" />.
        /// </exception>
        private static IEnumerable<KeyValuePair<string, object>> GetMetadata<TMetadataType>(Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException("targetType");
            }
            var attribute =
                (from p in targetType.GetCustomAttributes(typeof(TMetadataType), true) select p).FirstOrDefault();

            return attribute != null ? GetProperties(attribute, targetType) : new List<KeyValuePair<string, object>>();
        }
    }
}
