namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Factory interface for creating file service instances
/// </summary>
public interface IFileServiceFactory
{
    /// <summary>
    /// Create the appropriate file service based on configuration
    /// </summary>
    /// <returns>File service instance</returns>
    IFileService CreateFileService();
}
