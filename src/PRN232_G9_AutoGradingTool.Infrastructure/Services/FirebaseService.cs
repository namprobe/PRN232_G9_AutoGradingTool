using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

public class FirebaseService : IFirebaseService
{
    public Task<List<NotificationSendResult>> SendMultiCastAsync(NotificationRequest message, List<RecipientInfo> recipients)
    {
        throw new NotImplementedException();
    }

    public Task<NotificationSendResult> SendNotificationAsync(NotificationRequest message, RecipientInfo recipient)
    {
        throw new NotImplementedException();
    }
}