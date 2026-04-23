using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Query-side grading pipeline service for discovery and result parsing.
/// </summary>
public interface IGradingResultParser
{
  /// <summary>
  /// Detect Q1/Q2 project folders inside an extracted submission root.
  /// </summary>
  (string? q1, string? q2) DetectProjects(string root);

  /// <summary>
  /// Parse Newman JSON output into per-test result details.
  /// </summary>
  IReadOnlyList<ResultDetail> ParseNewmanTestResults(string newmanJson);
}