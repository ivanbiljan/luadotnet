using System;
using System.IO;
using LuaDotNet;

namespace LuaDotNet.Interactive {
    class Program {
        static void Main(string[] args) {
            using (var lua = new LuaContext()) {
                var input = string.Empty;
                do {
                    Console.Write("> ");
                    input = Console.ReadLine() ?? string.Empty;
                    try {
                        if (File.Exists(input)) {
                            lua.DoFile(input);
                        }
                        else {
                            lua.DoString(input);
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex);
                    }
                } while (!input.Equals("exit", StringComparison.InvariantCultureIgnoreCase));
            }
        }
    }
}