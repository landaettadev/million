namespace RealEstate.Application.Interfaces;

public interface IImageStorageService
{
    Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, CancellationToken ct = default);
    Task<bool> DeleteImageAsync(string blobName, CancellationToken ct = default);
    Task<Stream> DownloadImageAsync(string blobName, CancellationToken ct = default);
    Task<string> GetImageUrlAsync(string blobName, CancellationToken ct = default);
    Task<bool> ImageExistsAsync(string blobName, CancellationToken ct = default);
}
