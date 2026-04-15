using PRN232_G9_AutoGradingTool.Application.Common.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

public interface INotificationFactory
{
    INotificationService GetSender(NotificationChannelEnum channel);
}