using System.Diagnostics;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Command-side grading pipeline service for file extraction and process execution.
/// </summary>
public interface IGradingProcessService
{
  /// <summary>
  /// Extract a submission zip into a temporary working folder.
  /// </summary>
  string ExtractZip(string zipPath, string? workingDirectory = null);

  /// <summary>
  /// Start the student application on the requested local port.
  /// </summary>
  Process RunApp(string path, int port);

  /// <summary>
  /// Start Newman against the generated collection and base URL.
  /// </summary>
  Process RunNewman(string collectionJsonPath, string baseUrl, string? workingDirectory = null);

  /// <summary>
  /// Capture stdout/stderr after a process exits.
  /// </summary>
  Task<string> CaptureProcessOutputAsync(Process process, CancellationToken cancellationToken = default);

  /// <summary>
  /// Best-effort cleanup for the grading process and its temporary folder.
  /// </summary>
  void CleanupResources(Process? process, string? tempDirectory);
}