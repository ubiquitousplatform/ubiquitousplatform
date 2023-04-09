public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public enum SerializationFormat
{
    JSON,
    MessagePack
}

class Program
{
    static void Main(string[] args)
    {
        var user = new User
        {
            Username = "John Doe",
            Email = "john.doe@example.com",
        };

        var wasmInterop = new WasmInterop("ubiq_wasm.wasm");
        wasmInterop.CallUbiqWasm(user, SerializationFormat.JSON);
    }
}