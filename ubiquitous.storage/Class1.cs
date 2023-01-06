namespace ubiquitous.storage;

public interface IStorage
{
    byte[] GetObjectAsync(string storageId);
    void StoreObjectAsync(string storageId, byte[] data);

}
public class FilesystemStorage : IStorage
{
    public IDatabase _database;
    public FilesystemStorage(IDatabase database)
    {
        _database = database;
        _database.RegisterSchema();
    }
    public async byte[] GetObjectAsync(string storageId)
    {
        var basePath = "";
        var fullPath =
        if (File.Exists($"storage/{storageId}"))
        {
            return await File.ReadAllBytesAsync()
        }
    }

}

