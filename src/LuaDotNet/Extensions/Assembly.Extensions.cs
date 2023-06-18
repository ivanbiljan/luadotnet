using System;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;

namespace LuaDotNet.Extensions
{
    public static class AssemblyExtensions
    {
        public static string GetDirectory([NotNull] this Assembly assembly) =>
            Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(assembly.CodeBase).Path));
    }
}