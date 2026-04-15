using PRN232_G9_AutoGradingTool.Application.Common.Models;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Main notification service interface following Factory pattern
/// </summary>
public interface INotificationService
{
    Task<NotificationSendResult> SendNotificationAsync(NotificationRequest message, RecipientInfo recipient);
    Task<List<NotificationSendResult>> SendMultiCastAsync(NotificationRequest message, List<RecipientInfo> recipients);
}







