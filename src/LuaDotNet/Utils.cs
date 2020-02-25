using System;
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
        
        // https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/language-specification/overload-resolution
        public static MethodBase PickOverload(IEnumerable<MethodBase> candidates, object[] arguments, out object[] convertedArguments) {
            convertedArguments = null;
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
                if (explicitFactor > bestExplicitScore) {
                    bestExplicitScore = explicitFactor;
                    convertedArguments = args;
                    method = candidate;
                }
            }

            double CheckParameters(IReadOnlyCollection<ParameterInfo> parameters, out object[] args) {
                args = new object[parameters.Count];
                var explicitArgumentCount = 0;
                var implicitParameterCount = 0;
                for (var i = 0; i < parameters.Count; ++i) {
                    var parameter = parameters.ElementAt(i);
                    if (parameter.IsOut || parameter.ParameterType.IsByRef) {
                        ++implicitParameterCount;
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
                        var arrayType = parameter.ParameterType.GetElementType();
                        var array = Array.CreateInstance(arrayType, arguments.Length - i);
                        for (var j = 0; j < array.Length; ++j) {
                            if (!TryImplicitConversion(argument, arrayType, out var element)) {
                                return -1D;
                            }

                            array.SetValue(element, j);
                        }

                        args[i] = array;
                        ++implicitParameterCount;
                    }
                    else if (TryImplicitConversion(argument, parameter.ParameterType, out var obj)) {
                        args[i] = obj;
                        ++explicitArgumentCount;
                    }
                }


                // If the number of converted arguments does not match the number of arguments passed to the method call that either means
                // that at least one argument in the argument list is not applicable or there are not enough arguments provided
                if (/*convertedArgumentCount != arguments.Length || */explicitArgumentCount != parameters.Count - implicitParameterCount) {
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