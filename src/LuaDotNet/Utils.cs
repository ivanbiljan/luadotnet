﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using LuaDotNet.Extensions;

namespace LuaDotNet {
    internal static class Utils {
        [UsedImplicitly]
        public static object CoerceObjectMaybe(object obj, Type type) =>
            TryImplicitConversion(obj, type, out var resultObj) ? resultObj : obj;

        // TODO construct an overload resolution mechanism based on the specification from --> Done
        // https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/language-specification/overload-resolution
        public static MethodBase PickOverload(IEnumerable<MethodBase> candidates, object[] arguments, out object[] convertedArguments) {
            convertedArguments = new object[0];
            var bestExplicitScore = -1D;
            MethodBase method = null;
            foreach (var candidate in candidates) {
                if (candidate == null) {
                    continue;
                }
                
                var parameters = candidate.GetParameters();
                if (parameters.Length == 0 && arguments.Length == 0) {
                    return candidate;
                }
                
                if (candidate.IsGenericMethodDefinition) {
                    var genericParameters = candidate.GetGenericArguments();
                    var skip = genericParameters.Where((genericParameterType, i) => arguments[i].GetType() != genericParameterType).Any();
                    if (skip) {
                        continue;
                    }
                }

                if (parameters.Length < arguments.Length) {
                    if (parameters.Length != 0 && !parameters[parameters.Length - 1].IsParamsArray()) {
                        continue;
                    }
                }

                var explicitFactor = CheckParameters(parameters, out var args);
                Debug.WriteLine("Factor: " + explicitFactor);
                if (explicitFactor > bestExplicitScore) {
                    bestExplicitScore = explicitFactor;
                    convertedArguments = args;
                    method = candidate;
                }
            }

            double CheckParameters(IReadOnlyCollection<ParameterInfo> parameters, out object[] args) {
                args = new object[parameters.Count];
                var convertedArgumentCount = 0;
                var explicitArgumentCount = 0;
                var implicitParameterCount = 0;
                for (var i = 0; i < parameters.Count; ++i) {
                    var parameter = parameters.ElementAt(i);
                    if (parameter.IsOut) {
                        continue;
                    }
                    
                    var argument = arguments.ElementAtOrDefault(i);
                    if (argument == null) {
                        if (!parameter.IsOptional) {
                            break;
                        }

                        args[i] = parameter.DefaultValue;
                        ++implicitParameterCount;
                        continue;
                    }

                    if (parameter.IsParamsArray()) {
                        /* TODO implement this properly in the future
                           The current system relies on Lua tables when it comes to parsing array,
                           meaning that a MethodA(params object[] args) method has to be invoked like so: MethodA({1, 2, 3, ...})
                           Such mechanism defeats the purpose of a params type[] parameter
                         */
                    }

                    if (!TryImplicitConversion(argument, parameter.ParameterType, out var obj)) {
                        break;
                    }

                    args[i] = obj;
                    ++convertedArgumentCount;
                    ++explicitArgumentCount;
                }

                
                // If the number of converted arguments does not match the number of arguments passed to the method call that either means
                // that at least one argument in the argument list is not applicable or there are not enough arguments provided
                if (/*convertedArgumentCount != arguments.Length || */convertedArgumentCount != parameters.Count - implicitParameterCount) {
                    return -1D;
                }

                return (double) explicitArgumentCount / parameters.Count;
            }
            
            return method;
        }

        public static bool TryImplicitConversion(object obj, Type type, out object resultObj) {
            resultObj = obj;
            switch (obj) {
                case long _ when type.IsInteger():
                case double _ when type == typeof(float) || type == typeof(decimal):
                    resultObj = Convert.ChangeType(obj, type);
                    return true;
                case LuaTable luaTable when type.IsArray:
                    var arrayType = type.GetElementType();
                    var array = Array.CreateInstance(arrayType, luaTable.Count);
                    for (long i = 0; i < array.Length; ++i) {
                        if (!TryImplicitConversion(luaTable[i + 1], arrayType, out var temp)) {
                            return false;
                        }

                        array.SetValue(temp, i);
                    }

                    resultObj = array;
                    return true;
                default:
                    return type.IsInstanceOfType(obj);
            }
        }
    }
}