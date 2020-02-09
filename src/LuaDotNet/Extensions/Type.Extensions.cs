using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LuaDotNet.Extensions {
    /// <summary>
    ///     Provides extension methods for the <see cref="Type" /> type.
    /// </summary>
    public static class TypeExtensions {
        private static readonly ConditionalWeakTable<Type, TypeMetadata> MetadataCache =
            new ConditionalWeakTable<Type, TypeMetadata>();

        /// <summary>
        ///     Gets the extension methods for the specified type.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <returns>An enumerable collection of extension methods.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        public static IEnumerable<MethodInfo> GetExtensionMethods(this Type type) {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var extensionMethods = from a in assemblies
                let types = a.GetTypes()
                from t in types
                where t.IsDefined(typeof(ExtensionAttribute))
                from m in t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                where m.IsExtensionMethod() && m.GetParameters().ElementAt(0)?.ParameterType == type
                select m;

            return extensionMethods;
        }

        /// <summary>
        ///     Gets or creates type metadata for the specified type.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <returns>The metadata.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        public static TypeMetadata GetOrCreateMetadata(this Type type) {
            if (!MetadataCache.TryGetValue(type, out var metadata)) {
                metadata = TypeMetadata.Create(type);
                MetadataCache.Add(type, metadata);
            }

            return metadata;
        }

        /// <summary>
        ///     Returns a value indicating whether the specified type is an integer type.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <returns><c>true</c> if the type is an integer; otherwise, <c>false</c>.</returns>
        public static bool IsInteger(this Type type) {
            switch (Type.GetTypeCode(type)) {
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
            }

            return false;
        }
    }
}