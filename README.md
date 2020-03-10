# luadotnet
[![Travis](https://img.shields.io/travis/com/ivanbiljan/luadotnet?label=tests)](https://travis-ci.com/ivanbiljan/luadotnet) [![LICENSE](https://img.shields.io/github/license/ivanbiljan/luadotnet)](https://github.com/ivanbiljan/luadotnet/blob/master/LICENSE)

**luadotnet** is a .NET standard library that integrates the [Lua](https://lua.org) language into the .NET CLR. It provides an easy and effective way of embedding Lua scripts into .NET applications. It is built upon the .NET Standard API and targets Windows and Linux operating systems.

## Advantages
* Fully compatible with Lua 5.3.5
* Lua -> .NET access and vice-versa
* Managed control over Lua objects (functions, tables and coroutines)
* Supports extension method invocation using the colon (`:`) syntax
* Lua-side event handling 
* Dynamic scripting support

## Drawbacks
* It is not 100% accurate when it comes to resolving complex overloads
* It does not support generic type construction

## Example 
The following example demonstrates how to use luadotnet to output `Hello, World!` to a .NET console application
```csharp
using System;
using LuaDotNet;

namespace LuaDotNet.HelloWorld {
    class Program {
        static void Main(string[] args) {
            using (var lua = new Lua()) {
                lua.DoString("print('Hello, World!')");
            }
        }
    }
```

Take a look at the [unit tests](https://github.com/ivanbiljan/luadotnet/tree/master/tests/LuaDotNet.Tests) for a more profound insight to the API behind the project.

## To do list
* Support generic type instantiation
