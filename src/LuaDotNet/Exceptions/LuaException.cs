using System;
using JetBrains.Annotations;

namespace LuaDotNet.Exceptions
{
    /// <summary>
    ///     Represents a Lua exception.
    /// </summary>
    [PublicAPI]
    [Serializable]
    public sealed class LuaException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LuaException" /> class with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public LuaException(string message) : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LuaException" /> class with the specified message and inner exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public LuaException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}