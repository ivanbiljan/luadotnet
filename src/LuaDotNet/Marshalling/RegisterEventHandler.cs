using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using LuaDotNet.Exceptions;

namespace LuaDotNet.Marshalling {
    internal sealed class RegisterEventHandler {
        // ReSharper disable once CollectionNeverQueried.Local
        private static readonly Dictionary<LuaFunction, Delegate> EventHandlers = new Dictionary<LuaFunction, Delegate>();

        private readonly EventInfo _event;
        private readonly object _target;

        public RegisterEventHandler([NotNull] EventInfo eventInfo, object target) {
            _event = eventInfo ?? throw new ArgumentNullException(nameof(eventInfo));
            _target = target;
        }

        [UsedImplicitly]
        public void Add([NotNull] LuaFunction luaFunction) {
            if (luaFunction == null) {
                throw new ArgumentNullException(nameof(luaFunction));
            }

            var eventHandlerType = _event.EventHandlerType;
            if (eventHandlerType != typeof(EventHandler) && eventHandlerType != typeof(EventHandler<>)) {
                throw new LuaException("Cannot hook event with a non 'void(object, TEventArgs)' signature.");
            }

            // Type arguments have to be interpretable at compile time, so we have to resort to reflection
            var eventArgsType = eventHandlerType.GetMethod("Invoke")?.GetParameters()[1].ParameterType;
            var constructedWrapperType = typeof(LuaEventHandler<>).MakeGenericType(eventArgsType);
            var luaFunctionWrapper = Activator.CreateInstance(constructedWrapperType, luaFunction);
            var @delegate = Delegate.CreateDelegate(_event.EventHandlerType, luaFunctionWrapper, "HandleEvent");
            try {
                _event.AddEventHandler(_target, @delegate);
                EventHandlers[luaFunction] = @delegate;
            }
            catch (TargetInvocationException ex) {
                throw new LuaException($"An exception has occured while adding an event handler: {ex}");
            }
        }
    }
}