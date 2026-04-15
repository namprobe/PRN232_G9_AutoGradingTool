using Microsoft.Extensions.DependencyInjection;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Factories;

/// <summary>
/// Factory interface for notification services
/// </summary>
public class NotificationFactory : INotificationFactory
{
    private readonly IServiceProvider _serviceProvider;
    public NotificationFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public INotificationService GetSender(NotificationChannelEnum channel)
    {
        return channel switch
        {
            NotificationChannelEnum.Email => _serviceProvider.GetRequiredService<IEmailService>(),
            NotificationChannelEnum.Firebase => _serviceProvider.GetRequiredService<IFirebaseService>(),
            _ => throw new NotImplementedException($"Notification channel {channel} is not supported or temporarily disabled")
        };
    }
}
