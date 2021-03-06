/*
 * Copyright (c) 2016 Håkan Edling
 *
 * This software may be modified and distributed under the terms
 * of the MIT license.  See the LICENSE file for details.
 * 
 * https://github.com/piranhacms/piranha.core
 * 
 */

using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Piranha.Extend;

namespace Piranha.Builder.Attribute
{
    public abstract class ContentTypeBuilder<T, TType> where T : ContentTypeBuilder<T, TType> where TType : ContentType
    {
        #region Members
        protected readonly List<Type> types = new List<Type>();
        protected readonly IApi api;
        protected readonly ILogger logger;
        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="api">The current api</param>
        /// <param name="logFactory">The optional log factory</param>
        public ContentTypeBuilder(IApi api, ILoggerFactory logFactory = null) {
            this.api = api;
            this.logger = logFactory?.CreateLogger(this.GetType().FullName);
        }

        /// <summary>
        /// Adds a new type to build page types from
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The builder</returns>
        public T AddType(Type type) {
            types.Add(type);

            return (T)this;
        }

        /// <summary>
        /// Builds the page types.
        /// </summary>
        public abstract void Build();

        #region Private methods
        /// <summary>
        /// Gets the possible content type for the given type.
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The content type</returns>
        protected abstract TType GetContentType(Type type);

        /// <summary>
        /// Gets the possible region type for the given property.
        /// </summary>
        /// <param name="prop">The property info</param>
        /// <returns>The region type</returns>
        protected RegionType GetRegionType(PropertyInfo prop) {
            var attr = prop.GetCustomAttribute<RegionAttribute>();

            if (attr != null) {
                var isCollection = typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(prop.PropertyType);

                var regionType = new RegionType() {
                    Id = prop.Name,
                    Title = attr.Title,
                    Collection = isCollection,
                    Max = attr.Max,
                    Min = attr.Min
                };

                Type type = null;

                if (!isCollection) {
                    type = prop.PropertyType;
                } else {
                    type = prop.PropertyType.GenericTypeArguments.First();
                }

                if (typeof(IField).GetTypeInfo().IsAssignableFrom(type)) {
                    var appFieldType = App.Fields.GetByType(type);

                    if (appFieldType == null) {
                        // This is a single field region, but the type is missing.
                        // Discard the entire region
                        logger?.LogError($"Missing field type [{type.Name}] for region '{regionType.Id}'.");
                        return null;
                    }

                    regionType.Fields.Add(new FieldType() {
                        Id = "Default",
                        Type = appFieldType.CLRType
                    });
                } else {
                    foreach (var fieldProp in type.GetTypeInfo().GetProperties(App.PropertyBindings)) {
                        var fieldType = GetFieldType(fieldProp);

                        if (fieldType != null)
                            regionType.Fields.Add(fieldType);
                    }
                    // Skip regions without fields.
                    if (regionType.Fields.Count == 0)
                        return null;
                }
                return regionType;
            }
            return null;
        }

        /// <summary>
        /// Gets the possible field type for the given property.
        /// </summary>
        /// <param name="prop">The property</param>
        /// <returns>The field type</returns>
        protected FieldType GetFieldType(PropertyInfo prop) {
            var attr = prop.GetCustomAttribute<FieldAttribute>();

            if (attr != null) {
                var appFieldType = App.Fields.GetByType(prop.PropertyType);

                if (appFieldType != null) {
                    return new FieldType() {
                        Id = prop.Name,
                        Title = attr.Title,
                        Type = appFieldType.CLRType
                    };
                } else {
                    logger?.LogError($"Missing field type [{prop.PropertyType.Name}] for field '{prop.Name}'.");
                }
            }
            return null;
        }
        #endregion
    }
}
