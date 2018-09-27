﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Dapplo.Addons.Bootstrapper.AttributeMetaData
{
    /// <summary>
    /// Defines an attribute that provides custom metadata generation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The standard attribute metadata mechanism takes the names of public
    /// properties from an attribute and creates a dictionary of name/value
    /// pairs based on those properties, using that as the metadata for a
    /// service.
    /// </para>
    /// <para>
    /// When you need to provide a more robust attribute-based definition of
    /// metadata, you can instead have your metadata attribute implement this
    /// interface. Rather than using name/value pairs generated by the properties
    /// on your attribute, you can directly provide the metadata key/value pairs
    /// to associate with a service.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// This example shows what a metadata attribute might look like when providing
    /// custom metadata:
    /// </para>
    /// <code lang="C#">
    /// [MetadataAttribute]
    /// [AttributeUsage(AttributeTargets.Class)]
    /// public class ProvidedMetadataAttribute : Attribute, IMetadataProvider
    /// {
    ///   public IDictionary&lt;string, object&gt; GetMetadata(Type targetType)
    ///   {
    ///     return new Dictionary&lt;string, object&gt;()
    ///     {
    ///       { "Key1", "Value1" },
    ///       { "Key2", "Value2" }
    ///     };
    ///   }
    /// }
    /// </code>
    /// </example>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Gets metadata pairs for the passed target type.
        /// </summary>
        /// <param name="targetType">Target <see cref="Type"/> for which metadata should be retrieved.</param>
        /// <returns>Metadata dictionary to merge with existing metadata.</returns>
        IDictionary<string, object> GetMetadata(Type targetType);
    }
}
