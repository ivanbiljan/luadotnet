using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;

namespace LuaDotNet
{
    internal static class Utils
    {
        // TODO: Work out a better resolution mechanism
        // A shitty implementation based on https://stackoverflow.com/questions/5173339/how-does-the-method-overload-resolution-system-decide-which-method-to-call-when
        public static MethodBase TryResolveMethodCall(IEnumerable<MethodBase> candidates, object[] arguments,
            out object[] convertedArguments)
        {
            convertedArguments = new object[arguments.Length];
            
            var possibleOverloads = new List<MethodBase>();
            foreach (var candidate in candidates)
            {
                var parameters = candidate.GetParameters();
                if (parameters.Length != arguments.Length)
                {
                    continue;
                }

                var satisfies = true;
                for (var i = 0; i < parameters.Length; ++i)
                {
                    var parameter = parameters[i];
                    if (!TryImplicitConversion(arguments[i], parameter.ParameterType, out var argument))
                    {
                        satisfies = false;
                        break;
                    }

                    convertedArguments[i] = argument;
                }

                if (satisfies)
                {
                    possibleOverloads.Add(candidate);
                }
            }

            if (possibleOverloads.Count > 1)
            {
                throw new LuaException("Ambiguous method call");
            }

            return possibleOverloads.ElementAtOrDefault(0);
        }

        private static bool TryImplicitConversion(object obj, Type type, out object resultObj)
        {
            resultObj = obj;
            switch (obj)
            {
                case long _ when type.IsInteger():
                case double _ when type == typeof(float) || type == typeof(decimal):
                    resultObj = Convert.ChangeType(obj, type);
                    return true;
                default:
                    return type.IsInstanceOfType(obj);
            }
        }
    }
}