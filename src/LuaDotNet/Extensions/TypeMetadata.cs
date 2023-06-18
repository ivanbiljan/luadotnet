using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using LuaDotNet.Attributes;

namespace LuaDotNet.Extensions
{
    /// <summary>
    ///     Provides type metadata. Acts as a wrapper around a <see cref="Type" /> object and filters out unnecessary members.
    /// </summary>
    [PublicAPI]
    public sealed class TypeMetadata
    {
        private IList<ConstructorInfo> _constructors;
        private IList<EventInfo> _instanceEvents;
        private IList<FieldInfo> _instanceFields;
        private IList<MethodInfo> _instanceMethods;
        private IList<PropertyInfo> _instanceProperties;
        private IList<EventInfo> _staticEvents;
        private IList<FieldInfo> _staticFields;
        private IList<MethodInfo> _staticMethods;
        private IList<PropertyInfo> _staticProperties;

        /// <summary>
        ///     Gets an enumerable collection of the underlying type's constructors.
        /// </summary>
        public IEnumerable<ConstructorInfo> Constructors => _constructors;

        /// <summary>
        ///     Gets an enumerable collection of the underlying type's instance fields.
        /// </summary>
        public IEnumerable<FieldInfo> InstanceFields => _instanceFields;

        /// <summary>
        ///     Gets an enumerable collection of the underlying type's instance members.
        /// </summary>
        public IEnumerable<MemberInfo> InstanceMembers => _instanceEvents.Cast<MemberInfo>().Concat(_instanceFields)
            .Concat(_instanceMethods).Concat(_instanceProperties);

        /// <summary>
        ///     Gets an enumerable collection of the underlying type's instance methods.
        /// </summary>
        public IEnumerable<MethodInfo> InstanceMethods => _instanceMethods;

        /// <summary>
        ///     Gets an enumerable collection of the underlying type's instance properties.
        /// </summary>
        public IEnumerable<PropertyInfo> InstanceProperties => _instanceProperties;

        /// <summary>
        ///     Gets an enumerable collection of the underlying type's static fields.
        /// </summary>
        public IEnumerable<FieldInfo> StaticFields => _staticFields;

        /// <summary>
        ///     Gets an enumerable collection of the underlying type's static members.
        /// </summary>
        public IEnumerable<MemberInfo> StaticMembers => _staticEvents.Cast<MemberInfo>().Concat(_staticFields)
            .Concat(_staticMethods).Concat(_staticProperties);

        /// <summary>
        ///     Gets an enumerable collection of the underlying type's static methods.
        /// </summary>
        public IEnumerable<MethodInfo> StaticMethods => _staticMethods;

        /// <summary>
        ///     Gets an enumerable collection of the underlying type's static properties.
        /// </summary>
        public IEnumerable<PropertyInfo> StaticProperties => _staticProperties;

        /// <summary>
        ///     Creates metadata for the specified type. This filters members that are visible to the Lua runtime and ignores the
        ///     hidden ones.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <returns>The metadata.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <c>null</c>.</exception>
        public static TypeMetadata Create(Type type)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            var constructorInfos = type.GetConstructors().ToList();
            var methodInfos = new List<MethodInfo>();
            foreach (var method in type.GetMethods(bindingFlags))
            {
                if (method.GetCustomAttribute<LuaHideAttribute>() != null)
                {
                    continue;
                }

                // Disregard methods that wouldn't be of much use to the Lua runtime
                if (method.Name != "GetType" &&
                    method.Name != "GetHashCode" &&
                    method.Name != "Equals" &&
                    method.Name != "ToString" &&
                    method.Name != "Clone" &&
                    method.Name != "Dispose" &&
                    method.Name != "GetEnumerator" &&
                    method.Name != "CopyTo" &&
                    !method.Name.StartsWith("_get") &&
                    !method.Name.StartsWith("_set") &&
                    !method.Name.StartsWith("_add") &&
                    !method.Name.StartsWith("_remove"))
                {
                    methodInfos.Add(method);
                }
            }

            var eventInfos = type.GetEvents(bindingFlags).Where(e => e.GetCustomAttribute<LuaHideAttribute>() == null)
                .ToList();
            var fieldInfos = type.GetFields(bindingFlags).Where(f => f.GetCustomAttribute<LuaHideAttribute>() == null)
                .ToList();
            var propertyInfos = type.GetProperties(bindingFlags)
                .Where(p => p.GetCustomAttribute<LuaHideAttribute>() == null).ToList();

            return new TypeMetadata
            {
                _constructors = constructorInfos,
                _instanceEvents = eventInfos.Where(e => !e.AddMethod.IsStatic).ToList(),
                _instanceFields = fieldInfos.Where(f => !f.IsStatic).ToList(),
                _instanceMethods = methodInfos.Where(m => !m.IsStatic).Concat(type.GetExtensionMethods()).ToList(),
                _instanceProperties = propertyInfos.Where(p => !p.GetAccessors(false).Any(a => a.IsStatic)).ToList(),
                _staticEvents = eventInfos.Where(e => e.AddMethod.IsStatic).ToList(),
                _staticFields = fieldInfos.Where(f => f.IsStatic).ToList(),
                _staticMethods = methodInfos.Where(m => m.IsStatic).ToList(),
                _staticProperties = propertyInfos.Where(p => p.GetAccessors(false).Any(a => a.IsStatic)).ToList()
            };
        }

        /// <summary>
        ///     Returns an enumerable collection of the underlying type's members that match the specified name.
        /// </summary>
        /// <param name="memberName">The member name, which must not be <c>null</c>.</param>
        /// <param name="instance">The value indicating whether the member is an instance member.</param>
        /// <returns>An enumerable collection of members that match the specified name.</returns>
        public IEnumerable<MemberInfo> GetMembers(string memberName, bool instance = false)
        {
            return instance
                ? InstanceMembers.Where(m => m.Name == memberName)
                : StaticMembers.Where(m => m.Name == memberName);
        }

        /// <summary>
        ///     Returns an enumerable collection of the underlying type's methods that match the specified name.
        /// </summary>
        /// <param name="methodName">The method name, which must not be <c>null</c>.</param>
        /// <param name="instance">The value indicating whether the method is an instance method.</param>
        /// <returns>An enumerable collection of methods that match the specified name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="methodName" /> is <c>null</c>.</exception>
        public IEnumerable<MethodInfo> GetMethods(string methodName, bool instance = false)
        {
            return instance
                ? _instanceMethods.Where(m => m.Name == methodName)
                : _staticMethods.Where(m => m.Name == methodName);
        }
    }
}