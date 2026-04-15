using Microsoft.AspNetCore.Http;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Interface for different storage provider implementations
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Bulk upload files to storage
    /// </summary>
    /// <param name="files">List of files to upload</param>
    /// <param name="fileNames">List of file names to save as</param>
    /// <param name="subDirectory">Subdirectory within the storage (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of URLs or paths where the files are stored (null for failed uploads)</returns>
    Task<List<string?>> UploadFilesBulkAsync(List<IFormFile> files, List<string> fileNames, string subDirectory = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk delete files from storage
    /// </summary>
    /// <param name="filePaths">List of file paths to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bools indicating success for each file</returns>
    Task<List<bool>> DeleteFilesBulkAsync(List<string> filePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a file to storage
    /// </summary>
    /// <param name="file">File to upload</param>
    /// <param name="fileName">Name to save the file as</param>
    /// <param name="subDirectory">Subdirectory within the storage (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL or path where the file is stored</returns>
    Task<string> UploadFileAsync(IFormFile file, string fileName, string subDirectory = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a file to storage from a stream
    /// </summary>
    /// <param name="fileStream">Stream containing file data</param>
    /// <param name="fileName">Name to save the file as</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="subDirectory">Subdirectory within the storage (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL or path where the file is stored</returns>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string subDirectory = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    /// <param name="filePath">Path or identifier of the file to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the public URL for a file
    /// </summary>
    /// <param name="filePath">Path or identifier of the file</param>
    /// <returns>Public URL to access the file</returns>
    string GetFileUrl(string filePath);

    /// <summary>
    /// Get file content from storage
    /// </summary>
    /// <param name="filePath">Path or identifier of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of file content bytes and content type</returns>
    Task<(byte[] FileContent, string ContentType)> GetFileContentAsync(string filePath, CancellationToken cancellationToken = default);
}
