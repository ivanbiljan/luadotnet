﻿using System;

namespace LuaDotNet.Marshalling.Parsers
{
    /// <summary>
    ///     Describes a type parser.
    /// </summary>
    public interface ITypeParser
    {
        /// <summary>
        ///     Parses an object at the specified index in the stack of the specified Lua state.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="stackIndex">The Lua stack index.</param>
        /// <returns>The parsed value.</returns>
        object Parse(IntPtr state, int stackIndex);

        /// <summary>
        ///     Pushes an object to the specified Lua state.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The object</param>
        void Push(IntPtr state, object obj);
    }
}