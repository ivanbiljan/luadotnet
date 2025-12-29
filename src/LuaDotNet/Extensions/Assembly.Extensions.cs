using System;
using System.IO;
using System.Reflection;

namespace LuaDotNet.Extensions;

public static class AssemblyExtensions
{
    extension(Assembly assembly)
    {
        public string? GetDirectory()
        {
            return Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(assembly.Location).Path));
        }
    }
}