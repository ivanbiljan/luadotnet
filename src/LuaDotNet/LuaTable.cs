using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LuaDotNet.Extensions;
using LuaDotNet.Marshalling;
using LuaDotNet.PInvoke;

namespace LuaDotNet {
    /// <summary>
    /// Represents a Lua table.
    /// </summary>
    public sealed class LuaTable : LuaObject, IDictionary<object, object> {
        private readonly Dictionary<object, object> _dictionaryCtx;
        
        public LuaTable(LuaContext lua, int reference) : base(lua, reference) {
            _dictionaryCtx = new Dictionary<object, object>();
        }


        public IEnumerator<KeyValuePair<object, object>> GetEnumerator() => throw new System.NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(KeyValuePair<object, object> item) {
            Add(item.Key, item.Value);
        }

        public void Clear() {
            var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
            PushToStack(Lua.State);
            LuaModule.Instance.LuaPushNil(Lua.State);
            while (LuaModule.Instance.LuaNext(Lua.State, -2) != 0) {
                Remove(objectMarshal.GetObject(Lua.State, -2));
                LuaModule.Instance.LuaPop(Lua.State, 1);
            }
            
            _dictionaryCtx.Clear();
        }

        public bool Contains(KeyValuePair<object, object> item) => _dictionaryCtx.Contains(item);

        public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex) {
            throw new System.NotImplementedException();
        }

        public bool Remove(KeyValuePair<object, object> item) => Remove(item.Key);

        public int Count => _dictionaryCtx.Count;
        public bool IsReadOnly => false;
        public void Add(object key, object value) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
            PushToStack(Lua.State);
            objectMarshal.PushToStack(Lua.State, key);
            objectMarshal.PushToStack(Lua.State, value);
            LuaModule.Instance.LuaSetTable(Lua.State, -3);
            //LuaModule.Instance.LuaPop(Lua.State, 1);
            
            _dictionaryCtx.Add(key, value);
        }

        public bool ContainsKey(object key) => _dictionaryCtx.ContainsKey(key);

        public bool Remove(object key) {
            var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
            PushToStack(Lua.State);
            objectMarshal.PushToStack(Lua.State, key);
            objectMarshal.PushToStack(Lua.State, null);
            LuaModule.Instance.LuaRawSet(Lua.State, -2);

            _dictionaryCtx.Remove(key);
            return true;
        }

        public bool TryGetValue(object key, out object value) => _dictionaryCtx.TryGetValue(key, out value);

        public object this[object key] {
            get => _dictionaryCtx.TryGetValue(key, out var value) ? value : null;
            set => throw new System.NotImplementedException();
        }

        public ICollection<object> Keys => _dictionaryCtx.Keys;
        public ICollection<object> Values => _dictionaryCtx.Values;
    }
}