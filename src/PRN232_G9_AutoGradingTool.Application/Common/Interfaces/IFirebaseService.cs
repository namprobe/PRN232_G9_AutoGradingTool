namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Extended Firebase service interface
/// </summary>
public interface IFirebaseService : INotificationService
{
    // Task<NotificationChannelResult> SendToDeviceAsync(string deviceToken, string title, string body, Dictionary<string, object>? data = null);
    // Task<List<NotificationChannelResult>> SendToDevicesAsync(List<string> deviceTokens, string title, string body, Dictionary<string, object>? data = null);
    // Task<bool> VerifyDeviceTokenAsync(string deviceToken);
}