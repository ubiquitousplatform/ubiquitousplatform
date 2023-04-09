using System;
using System.Text.Json;
using System.Numerics;
using Wasmtime;

public class WasmInterop
{
    private Linker linker;
    private Instance instance;

    public WasmInterop(string wasmFilePath)
    {
        var engine = new Engine();
        var module = Module.FromFile(engine, wasmFilePath);

        var store = new Store(engine);

        linker = new Linker(engine);
        linker.DefineFunction(
            "",
            "my_host_function",
            (int enumValue, int dataLength, long dataPointer) =>
            {
                Console.WriteLine($"Enum value: {enumValue}");
                Console.WriteLine($"Data length: {dataLength}");

                var data = new byte[dataLength];
                
                // System.Runtime.InteropServices.Marshal.Copy(dataPointer, data, 0, dataLength);

                Console.WriteLine($"Data: {System.Text.Encoding.UTF8.GetString(data)}");

                var json = System.Text.Encoding.UTF8.GetString(data);
                var user = JsonSerializer.Deserialize<User>(json);

                Console.WriteLine($"Username: {user.Username}");
                Console.WriteLine($"User email: {user.Email}");

                return 42;
            }
        );
        linker.DefineWasi();
        instance = linker.Instantiate(store, module);
    }

    public void CallUbiqWasm(User user, SerializationFormat format)
    {
        // Serialize user to JSON or MessagePack
        byte[] data = null;
        switch (format)
        {
            case SerializationFormat.JSON:
                data = JsonSerializer.SerializeToUtf8Bytes(user);
                break;
            //case SerializationFormat.MessagePack:
            //    data = MessagePack.MessagePackSerializer.Serialize(user);
            //    break;
        }

        var enumValue = (int)format;
        //var dataLength = data.Length;
        //var dataPointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(dataLength);
        //System.Runtime.InteropServices.Marshal.Copy(data, 0, dataPointer, dataLength);

        var ubiqWasm = instance.GetFunction("call_ubiq_from_wasm");
        ubiqWasm.Invoke(enumValue);
        instance.
        //System.Runtime.InteropServices.Marshal.FreeHGlobal(dataPointer);
    }
}