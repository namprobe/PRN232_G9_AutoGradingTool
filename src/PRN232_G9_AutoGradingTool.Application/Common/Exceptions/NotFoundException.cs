using PRN232_G9_AutoGradingTool.Application.Common.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Exceptions;

/// <summary>
/// Thrown when an entity is not found.
/// Middleware maps this to 404 NotFound via ErrorCodeEnum.NotFound.
/// </summary>
public class NotFoundException : Exception
{
    public ErrorCodeEnum ErrorCode { get; } = ErrorCodeEnum.NotFound;

    /// <summary>
    /// Entity type for building descriptive messages (e.g. typeof(Exam) → "Exam not found").
    /// </summary>
    public Type? EntityType { get; }

    public NotFoundException(Type? entityType = null)
        : base(string.Empty)
    {
        EntityType = entityType;
    }

    public NotFoundException(string message, Type? entityType = null)
        : base(message)
    {
        EntityType = entityType;
    }
}
