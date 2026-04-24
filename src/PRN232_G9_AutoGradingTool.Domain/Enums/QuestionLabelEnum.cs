namespace PRN232_G9_AutoGradingTool.Domain.Enums;

/// <summary>
/// Labels for exam questions. Stored as string ("Q1", "Q2") in the database
/// but enforced via enum at the API boundary to prevent free-form input.
/// </summary>
public enum QuestionLabelEnum
{
    Q1,
    Q2
}
