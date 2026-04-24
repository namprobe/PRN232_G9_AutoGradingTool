using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

/// <summary>
/// Implements the grading pipeline helper services for extraction, process execution,
/// folder detection, and Newman result parsing.
/// </summary>
public sealed class ZipExtractionHelper : IGradingProcessService, IGradingResultParser
{
    /// <summary>
    /// Extracts a submission zip file into a unique temporary folder while protecting
    /// against zip-slip path traversal attacks.
    /// <para>
    /// The function validates the zip path, creates a dedicated extraction root, and
    /// expands each archive entry only if the resolved destination stays inside the
    /// intended extraction directory. Directory entries are created as folders, while
    /// file entries are written to disk with overwrite enabled so repeated grading runs
    /// can reuse the same helper safely.
    /// </para>
    /// </summary>
    /// <param name="zipPath">Absolute or relative path to the zip archive that contains the student submission.</param>
    /// <param name="workingDirectory">Optional base folder used to store extracted submissions. If omitted, a temp folder is created.</param>
    /// <returns>The full path to the newly created extraction folder.</returns>
    public string ExtractZip(string zipPath, string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
            throw new ArgumentException("Zip path is required.", nameof(zipPath));

        if (!File.Exists(zipPath))
            throw new FileNotFoundException("Zip file was not found.", zipPath);

        var rootDirectory = string.IsNullOrWhiteSpace(workingDirectory)
            ? Path.Combine(Path.GetTempPath(), "PRN232_G9_AutoGradingTool", "submissions")
            : workingDirectory;

        Directory.CreateDirectory(rootDirectory);

        var extractPath = Path.Combine(rootDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extractPath);

        var normalizedExtractRoot = Path.GetFullPath(extractPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.FullName))
                continue;

            var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

            // Prevent zip-slip path traversal when extracting student submissions.
            if (!destinationPath.StartsWith(normalizedExtractRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Zip entry '{entry.FullName}' resolves outside the extraction folder.");

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            entry.ExtractToFile(destinationPath, overwrite: true);
        }

        return extractPath;
    }

    /// <summary>
    /// Detects the extracted Q1 and Q2 project folders by scanning the top-level
    /// directories inside the submission root.
    /// <para>
    /// The function expects the archive to be extracted into a structure where project
    /// folders are named with prefixes such as <c>Q1_</c> and <c>Q2_</c>. It returns
    /// the first matching folder for each prefix, allowing the grading pipeline to
    /// locate the two question projects without hard-coding exact folder names.
    /// </para>
    /// </summary>
    /// <param name="root">Root directory that contains the extracted submission contents.</param>
    /// <returns>A tuple containing the detected Q1 and Q2 folder paths, or null values when a folder is missing.</returns>
    public (string? q1, string? q2) DetectProjects(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root path is required.", nameof(root));

        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Root directory was not found: {root}");

        var directories = Directory.GetDirectories(root);

        var q1 = directories.FirstOrDefault(directory =>
            Path.GetFileName(directory).StartsWith("Q1_", StringComparison.OrdinalIgnoreCase));

        var q2 = directories.FirstOrDefault(directory =>
            Path.GetFileName(directory).StartsWith("Q2_", StringComparison.OrdinalIgnoreCase));

        return (q1, q2);
    }

    /// <summary>
    /// Starts the student's published .NET application by locating the first runnable DLL
    /// in the given folder and launching it through <c>dotnet</c> on the requested port.
    /// <para>
    /// The method verifies that the folder exists, ensures the port is valid, selects a
    /// DLL that is not a resource assembly, and redirects stdout and stderr so the caller
    /// can capture runtime logs during grading.
    /// </para>
    /// </summary>
    /// <param name="path">Folder that contains the student's published application output.</param>
    /// <param name="port">Local port used to expose the application for Newman requests.</param>
    /// <returns>The started process handle for the student application.</returns>
    public Process RunApp(string path, int port)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"App folder was not found: {path}");

        if (port <= 0)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be greater than zero.");

        var dll = Directory.GetFiles(path, "*.dll")
            .FirstOrDefault(file => !Path.GetFileNameWithoutExtension(file).EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException("No runnable DLL was found in the published folder.", path);

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{dll}\" --urls=http://localhost:{port}",
            WorkingDirectory = path,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        return process ?? throw new InvalidOperationException("Failed to start the student app process.");
    }

    /// <summary>
    /// Starts a Newman process that executes the generated Postman collection against the
    /// student application base URL.
    /// <para>
    /// The helper validates the collection file, resolves a working directory, and starts
    /// Newman with the JSON reporter enabled so the grading pipeline can later parse the
    /// machine-readable execution results.
    /// </para>
    /// </summary>
    /// <param name="collectionJsonPath">Path to the generated Postman collection JSON file.</param>
    /// <param name="baseUrl">Base URL of the running student application.</param>
    /// <param name="workingDirectory">Optional working directory used when starting Newman.</param>
    /// <returns>The started Newman process handle.</returns>
    public Process RunNewman(string collectionJsonPath, string baseUrl, string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(collectionJsonPath))
            throw new ArgumentException("Collection path is required.", nameof(collectionJsonPath));

        if (!File.Exists(collectionJsonPath))
            throw new FileNotFoundException("Collection JSON was not found.", collectionJsonPath);

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL is required.", nameof(baseUrl));

        var rootDirectory = string.IsNullOrWhiteSpace(workingDirectory)
            ? Path.GetDirectoryName(collectionJsonPath) ?? Environment.CurrentDirectory
            : workingDirectory;

        Directory.CreateDirectory(rootDirectory);

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "newman",
            Arguments = $"run \"{collectionJsonPath}\" --reporters json --env-var baseUrl=\"{baseUrl}\"",
            WorkingDirectory = rootDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        return process ?? throw new InvalidOperationException("Failed to start the Newman process.");
    }

    /// <summary>
    /// Reads stdout and stderr from a running process and returns the most useful
    /// available text after the process exits.
    /// <para>
    /// The method waits for completion, then prefers standard output when present and
    /// falls back to standard error when output is empty. This makes it suitable for
    /// capturing Newman JSON output or diagnostic failure text from student app runs.
    /// </para>
    /// </summary>
    /// <param name="process">The process whose output should be captured.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for process completion.</param>
    /// <returns>The captured process output text, trimmed of surrounding whitespace.</returns>
    public async Task<string> CaptureProcessOutputAsync(Process process, CancellationToken cancellationToken = default)
    {
        if (process == null)
            throw new ArgumentNullException(nameof(process));

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (!string.IsNullOrWhiteSpace(standardOutput))
            return standardOutput.Trim();

        return standardError.Trim();
    }

    /// <summary>
    /// Parses the Newman JSON report and converts each execution item into a compact
    /// result record used by the grading UI and persistence layer.
    /// <para>
    /// The parser walks through <c>run.executions</c>, extracts the item name, response
    /// time, assertion outcomes, and failure messages, then maps each execution to a
    /// <see cref="ResultDetail"/> with a pass/fail flag, per-test score, and raw JSON.
    /// If assertions are missing, the execution is treated as passed, which matches the
    /// current grading contract for tests that only validate request/response presence.
    /// </para>
    /// </summary>
    /// <param name="newmanJson">The JSON text produced by Newman's JSON reporter.</param>
    /// <returns>A read-only list of parsed test result details, one item per execution.</returns>
    public IReadOnlyList<ResultDetail> ParseNewmanTestResults(string newmanJson)
    {
        // Be defensive in production grading runs: if Newman fails noisily or returns
        // empty output, treat it as "no executions" so the job can complete with 0 score
        // instead of crashing and keeping submission in Failed forever.
        if (string.IsNullOrWhiteSpace(newmanJson))
            return Array.Empty<ResultDetail>();

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(newmanJson);
        }
        catch
        {
            return Array.Empty<ResultDetail>();
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("run", out var runElement) ||
                !runElement.TryGetProperty("executions", out var executionsElement) ||
                executionsElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<ResultDetail>();
            }

            var results = new List<ResultDetail>();
            foreach (var execution in executionsElement.EnumerateArray())
            {
                var itemName = TryGetString(execution, "item", "name") ?? "Unknown testcase";
                var responseTime = TryGetInt32(execution, "response", "responseTime");

                var assertions = TryGetArray(execution, "assertions");
                var assertionEntries = assertions?.EnumerateArray().ToList() ?? new List<JsonElement>();
                var passed = assertionEntries.Count == 0 || assertionEntries.All(assertion =>
                    !assertion.TryGetProperty("success", out var successElement) || successElement.ValueKind == JsonValueKind.True);

                var failures = assertionEntries
                    .Where(assertion => assertion.TryGetProperty("success", out var successElement) && successElement.ValueKind == JsonValueKind.False)
                    .Select(assertion => TryGetString(assertion, "error", "message")
                        ?? TryGetString(assertion, "error", "name")
                        ?? TryGetString(assertion, "assertion")
                        ?? "Assertion failed")
                    .ToList();

                var rawOutputJson = execution.GetRawText();
                results.Add(new ResultDetail(
                    itemName,
                    passed,
                    passed ? 1d : 0d,
                    responseTime ?? 0,
                    failures.Count == 0 ? string.Empty : string.Join("; ", failures),
                    rawOutputJson));
            }

            return results;
        }
    }

    /// <summary>
    /// Attempts to stop any running grading process and delete its temporary working folder.
    /// <para>
    /// Cleanup is deliberately best-effort: the method swallows failures caused by already
    /// exited processes or locked files so the grading flow can finish cleanly even when
    /// the operating system has not released every handle yet.
    /// </para>
    /// </summary>
    /// <param name="process">The process to terminate, if it is still running.</param>
    /// <param name="tempDirectory">The temporary directory to delete after processing completes.</param>
    public void CleanupResources(Process? process, string? tempDirectory)
    {
        if (process != null)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);

                process.WaitForExit(5000);
            }
            catch
            {
                // Best-effort cleanup: the caller should not fail the grading flow because the process already exited.
            }
            finally
            {
                process.Dispose();
            }
        }

        if (!string.IsNullOrWhiteSpace(tempDirectory) && Directory.Exists(tempDirectory))
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup: temporary grading folders should not block completion if files are locked.
            }
        }
    }

    private static string? TryGetString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                return null;
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.GetRawText();
    }

    private static int? TryGetInt32(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                return null;
        }

        return current.ValueKind == JsonValueKind.Number && current.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static JsonElement? TryGetArray(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                return null;
        }

        return current.ValueKind == JsonValueKind.Array ? current : null;
    }
}