using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LuaDotNet.Marshalling;
using LuaDotNet.PInvoke;

namespace LuaDotNet;

/// <summary>
///     Represents a Lua table.
/// </summary>
public sealed class LuaTable : LuaObject, IDictionary<object, object>
{
    private readonly Dictionary<object, object> _dictionaryCtx = new();

    public LuaTable(LuaContext lua, int reference) : base(lua, reference)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(lua.State);
        PushToStack(Lua.State);
        LuaModule.LuaPushNil(Lua.State);
        while (LuaModule.LuaNext(Lua.State, -2) != 0)
        {
            _dictionaryCtx.Add(objectMarshal.GetObject(lua.State, -2), objectMarshal.GetObject(lua.State, -1));
            LuaModule.LuaPop(Lua.State, 1);
        }
    }

    public bool Contains(KeyValuePair<object, object> item)
    {
        return _dictionaryCtx.Contains(item);
    }

    public bool ContainsKey(object key)
    {
        return _dictionaryCtx.ContainsKey(key);
    }

    public bool IsReadOnly => false;

    public bool Remove(KeyValuePair<object, object> item)
    {
        return Remove(item.Key);
    }

    public bool Remove(object key)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
        PushToStack(Lua.State);
        objectMarshal.PushToStack(Lua.State, key);
        objectMarshal.PushToStack(Lua.State, null);
        LuaModule.LuaRawSet(Lua.State, -3);

        _dictionaryCtx.Remove(key);

        return true;
    }

    public bool TryGetValue(object key, out object value)
    {
        return _dictionaryCtx.TryGetValue(key, out value);
    }

    public ICollection<object> Keys => _dictionaryCtx.Keys;

    public ICollection<object> Values => _dictionaryCtx.Values;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
    {
        return _dictionaryCtx.GetEnumerator();
    }

    public int Count => _dictionaryCtx.Count;

    public object this[object key]
    {
        get => _dictionaryCtx.TryGetValue(key, out var value) ? value : null;
        set
        {
            if (_dictionaryCtx.ContainsKey(key))
            {
                if (_dictionaryCtx[key] == value)
                {
                    return;
                }

                if (value == null)
                {
                    Remove(key);

                    return;
                }

                var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
                PushToStack(Lua.State);
                objectMarshal.PushToStack(Lua.State, key);
                objectMarshal.PushToStack(Lua.State, value);
                LuaModule.LuaSetTable(Lua.State, -3);
                _dictionaryCtx[key] = value;
            }
            else
            {
                _dictionaryCtx.Add(key, value);
            }
        }
    }

    public void Add(KeyValuePair<object, object> item)
    {
        Add(item.Key, item.Value);
    }

    public void Add(object key, object value)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(Lua.State);
        PushToStack(Lua.State);
        objectMarshal.PushToStack(Lua.State, key);
        objectMarshal.PushToStack(Lua.State, value);
        LuaModule.LuaRawSet(Lua.State, -3);

        _dictionaryCtx.Add(key, value);
    }

    public void Clear()
    {
        PushToStack(Lua.State);
        LuaModule.LuaPushNil(Lua.State);
        while (LuaModule.LuaNext(Lua.State, -2) != 0)
        {
            LuaModule.LuaPushValue(Lua.State, -2); // key
            LuaModule.LuaPushNil(Lua.State); // value
            LuaModule.LuaRawSet(Lua.State, -5); // t[key] = nil
            LuaModule.LuaPop(Lua.State, 1); // pop the value, leave the key
        }

        _dictionaryCtx.Clear();
    }

    public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }
}