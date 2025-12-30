using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LuaDotNet.PInvoke;

namespace LuaDotNet;

/// <summary>
///     Represents a Lua table.
/// </summary>
public sealed class LuaTable : LuaObject, IDictionary<object, object>
{
    private readonly Dictionary<object, object> _dictionaryCtx = new();

    public LuaTable(LuaContext context, int reference) : base(context, reference)
    {
        var objectMarshal = ObjectMarshalPool.GetMarshal(context.State);
        PushToStack(Context.State);
        Lua.LuaPushNil(Context.State);
        while (Lua.LuaNext(Context.State, -2) != 0)
        {
            _dictionaryCtx.Add(objectMarshal.GetObject(context.State, -2), objectMarshal.GetObject(context.State, -1));
            Lua.LuaPop(Context.State, 1);
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
        var objectMarshal = ObjectMarshalPool.GetMarshal(Context.State);
        PushToStack(Context.State);
        objectMarshal.PushToStack(Context.State, key);
        objectMarshal.PushToStack(Context.State, null);
        Lua.LuaRawSet(Context.State, -3);

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

                var objectMarshal = ObjectMarshalPool.GetMarshal(Context.State);
                PushToStack(Context.State);
                objectMarshal.PushToStack(Context.State, key);
                objectMarshal.PushToStack(Context.State, value);
                Lua.LuaSetTable(Context.State, -3);
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
        var objectMarshal = ObjectMarshalPool.GetMarshal(Context.State);
        PushToStack(Context.State);
        objectMarshal.PushToStack(Context.State, key);
        objectMarshal.PushToStack(Context.State, value);
        Lua.LuaRawSet(Context.State, -3);

        _dictionaryCtx.Add(key, value);
    }

    public void Clear()
    {
        PushToStack(Context.State);
        Lua.LuaPushNil(Context.State);
        while (Lua.LuaNext(Context.State, -2) != 0)
        {
            Lua.LuaPushValue(Context.State, -2); // key
            Lua.LuaPushNil(Context.State); // value
            Lua.LuaRawSet(Context.State, -5); // t[key] = nil
            Lua.LuaPop(Context.State, 1); // pop the value, leave the key
        }

        _dictionaryCtx.Clear();
    }

    public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }
}