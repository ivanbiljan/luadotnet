using JetBrains.Annotations;

namespace LuaDotNet.PInvoke
{
    /// <summary>
    ///     Holds constants related to the Lua registry. These values are constants pulled from the lua.h file.
    /// </summary>
    [PublicAPI]
    public enum LuaRegistry
    {
        /// <summary>
        ///     Gets the registry's index.
        /// </summary>
        RegistryIndex = -1001000,

        /// <summary>
        ///     Gets the predefined registry value of the main thread (the thread created with the main state).
        /// </summary>
        MainThreadIndex = 1,

        /// <summary>
        ///     Gets the predefined registry value of the global environment.
        /// </summary>
        GlobalsIndex = 2
    }
}