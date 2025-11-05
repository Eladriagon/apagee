namespace Apagee.Services;

public interface IFileService
{
    public Task<string> SaveFile(byte[] contents, string? mimeType, string? folderName = null);
    public Task<(byte[] file, string mimeType)> GetFile(string name);
}