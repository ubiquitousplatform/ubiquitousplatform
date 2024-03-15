namespace ubiquitous.files;

public interface IStorage
{
    Task<byte[]> GetObjectAsync(string storageId);
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
    public async Task<byte[]> GetObjectAsync(string storageId)
    {
        var basePath = "";
        var fullPath = $"storage/{storageId}";
        if (File.Exists(fullPath))
        {
            return await File.ReadAllBytesAsync(fullPath);
        }
        return null
        ;
    }

    public void StoreObjectAsync(string storageId, byte[] data)
    {
        throw new NotImplementedException();
    }
}

