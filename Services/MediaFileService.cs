namespace Apagee.Services;

public class MediaFileService : IFileService
{
    private const string MIME_PNG = "image/png";
    private const string MIME_JPEG = "image/jpeg";
    private const string MIME_GIF = "image/gif";
    private const string MIME_WEBP = "image/webp";

    private readonly string[] ALLOWED_MIME_TYPES = [MIME_PNG, MIME_JPEG, MIME_GIF, MIME_WEBP];

    public async Task<string> SaveFile(byte[] contents, string? mimeType, string? folderName = null)
    {
        if (mimeType is not { Length: > 0 } || ALLOWED_MIME_TYPES.All(mt => !mt.IEquals(mimeType?.Trim().ToLower() ?? "")))
        {
            throw new ApageeException("Unsupported MIME type pasted: " + mimeType);
        }

        if (contents.Length == 0)
        {
            throw new ApageeException("No file contents to save.");
        }

        // Article directory may not exist.
        if (folderName is not null)
        {
            Directory.CreateDirectory(Path.Combine(Globals.MediaDir, folderName));
        }

        var filename = Path.Combine(Globals.MediaDir, $"{folderName}/{Ulid.NewUlid()}{GetExtensionForType(mimeType)}".TrimStart('/'));

        await File.WriteAllBytesAsync(filename, contents);

        // Special (and default) case where we need to remove this path from the start.
        return Path.GetRelativePath(Environment.CurrentDirectory, filename).Replace("\\", "/").TrimStart('/').Replace("wwwroot", "");
    }

    public async Task<(byte[] file, string mimeType)> GetFile(string name)
    {
        try
        {
            var fi = new FileInfo(name);
            if (!fi.Exists)
            {
                throw new FileNotFoundException("File not found: " + name);
            }
            return (await File.ReadAllBytesAsync(name), GetTypeForExtension(fi.Extension));
        }
        catch (Exception ex)
        {
            throw new ApageeException("Failed to get file by name: " + name, ex);
        }
    }

    private string GetExtensionForType(string mimeType) =>
        mimeType switch
        {
            MIME_PNG => ".png",
            MIME_JPEG => ".jpg",
            MIME_GIF => ".gif",
            MIME_WEBP => ".webp",
            _ => throw new ApageeException("Can't get extension for unknown mime type: " + mimeType)
        };

    private string GetTypeForExtension(string fileExt) =>
        fileExt.ToLower().TrimEnd('.') switch
        {
            "png" => MIME_PNG,
            "jpg" => MIME_JPEG,
            "gif" => MIME_GIF,
            "webp" => MIME_WEBP,
            _ => throw new ApageeException("Can't get mime type for unknown extension: " + fileExt)
        };
}