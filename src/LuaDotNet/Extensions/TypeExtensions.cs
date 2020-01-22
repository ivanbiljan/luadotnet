using System;

namespace LuaDotNet.Extensions
{
    /// <summary>
    ///     Provides extension methods for the <see cref="Type" /> type.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        ///     Returns a value indicating whether the specified type is an integer type.
        /// </summary>
        /// <param name="type">The type, which must not be <c>null</c>.</param>
        /// <returns><c>true</c> if the type is an integer; otherwise, <c>false</c>.</returns>
        public static bool IsInteger(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
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