﻿using System;
using System.Linq;
using System.Reflection;
using LuaDotNet.Exceptions;
using LuaDotNet.Extensions;
using LuaDotNet.PInvoke;

namespace LuaDotNet.Marshalling
{
    internal sealed class MethodWrapper
    {
        private readonly bool _isStatic;
        private readonly string _methodName; // Debugging purposes only
        private readonly MethodInfo[] _methods;
        private readonly object _target;

        public MethodWrapper(string methodName, Type type, object target = null)
        {
            _isStatic = target == null;
            _methodName = methodName;
            _methods = type.GetOrCreateMetadata().GetMethods(methodName, target != null).ToArray();
            _target = target;
        }

        public MethodWrapper(MethodInfo method, object target = null)
        {
            _isStatic = true;
            _methods = new[]
            {
                method
            };
            _target = target;
        }

        public int Callback(IntPtr state)
        {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            var args = objectMarshal.GetObjects(state, _isStatic ? 1 : 2, LuaModule.Instance.LuaGetTop(state));
            var method = Utils.PickOverload(_methods, args, out args);
            if (method == null)
            {
                throw new LuaException($"Cannot resolve method call: {_methodName}");
            }

            if (method.IsGenericMethodDefinition)
            {
                return GenericMethodCallback(state, method, args);
            }

            return InvokeAndPushResults(state, method, args);
        }

        private int GenericMethodCallback(IntPtr state, MethodInfo method, object[] args)
        {
            var genericParameters = method.GetGenericMethodDefinition().GetParameters();
            if (genericParameters.Length > args.Length)
            {
                throw new LuaException("Invalid generic method syntax.");
            }

            var typeArgs = new Type[genericParameters.Length];
            for (var i = 0; i < genericParameters.Length; ++i)
            {
                var parameter = genericParameters[i];
                if (!(args[i] is Type argType))
                {
                    throw new LuaException("Expected a type parameter.");
                }

                if (parameter.ParameterType.IsSubclassOf(argType))
                {
                    throw new LuaException("Invalid type argument.");
                }

                typeArgs[i] = argType;
            }

            method = method.MakeGenericMethod(typeArgs);
            args = args.Skip(genericParameters.Length).ToArray();

            return InvokeAndPushResults(state, method, args);
        }

        private int InvokeAndPushResults(IntPtr state, MethodInfo method, params object[] args)
        {
            var objectMarshal = ObjectMarshalPool.GetMarshal(state);
            object result;
            try
            {
                if (method.IsExtensionMethod())
                {
                    result = method.Invoke(
                        null,
                        new[]
                        {
                            _target
                        }.Concat(args).ToArray());
                }
                else
                {
                    result = method.Invoke(_target, args);
                }
            }
            catch (TargetInvocationException ex)
            {
                throw new LuaException($"An exception has occured while calling method '{method.Name}': {ex}");
            }

            var numberOfResults = 0;
            if (method.ReturnType != typeof(void))
            {
                objectMarshal.PushToStack(state, result);
                ++numberOfResults;
            }

            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; ++i)
            {
                var parameter = parameters[i];
                if (!parameter.IsOut && !parameter.ParameterType.IsByRef)
                {
                    continue;
                }

                objectMarshal.PushToStack(state, args[parameter.Position]);
                ++numberOfResults;
            }

            return numberOfResults;
        }
    }
}