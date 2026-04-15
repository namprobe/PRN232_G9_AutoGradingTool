using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Application.Common.Helpers;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public Task<List<NotificationSendResult>> SendMultiCastAsync(NotificationRequest message, List<RecipientInfo> recipients)
    {
        throw new NotImplementedException();
    }

    public async Task<NotificationSendResult> SendNotificationAsync(NotificationRequest message, RecipientInfo recipient)
    {
        try
        {
            // Build content by template if not explicitly provided
            var (subject, htmlContent) = BuildEmailContent(message, recipient);

            // Send email using SMTP client
            using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
            {
                client.Credentials = new System.Net.NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                client.EnableSsl = _emailSettings.EnableSsl;
                client.Timeout = 30000; // 30 seconds timeout

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = htmlContent,
                    IsBodyHtml = true,
                };

                if (string.IsNullOrEmpty(recipient.Email))
                {
                    throw new ArgumentException("Recipient email cannot be null or empty", nameof(recipient.Email));
                }

                mailMessage.To.Add(new MailAddress(recipient.Email, recipient.FullName ?? string.Empty));

                await client.SendMailAsync(mailMessage);
                
                _logger.LogInformation("Email queued successfully to {Email} with subject {Subject}", recipient.Email, subject);
                
                return new NotificationSendResult
                {
                    UserId = recipient.UserId ?? Guid.Empty,
                    Email = recipient.Email,
                    FullName = recipient.FullName,
                    ChannelResults = new List<NotificationChannelResult>
                    {
                        new NotificationChannelResult
                        {
                            Channel = NotificationChannelEnum.Email,
                            Success = true,
                            Message = "Email queued for delivery. Note: Delivery confirmation requires bounce handling.",
                            Timestamp = DateTime.UtcNow
                        }
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", recipient.Email, message.Subject);
            return new NotificationSendResult
            {
                UserId = recipient.UserId ?? Guid.Empty,
                Email = recipient.Email,
                FullName = recipient.FullName,
                ChannelResults = new List<NotificationChannelResult>
                {
                    new NotificationChannelResult
                    {
                        Channel = NotificationChannelEnum.Email,
                        Success = false,
                        Message = "Failed to send email",
                        ErrorMessage = ex.Message,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };
        }
    }

    private (string Subject, string HtmlContent) BuildEmailContent(NotificationRequest message, RecipientInfo recipient)
    {
        // If content already provided, use it directly
        var existingHtml = message.HtmlContent ?? message.Content;
        var existingSubject = message.Subject;
        if (!string.IsNullOrEmpty(existingHtml) && !string.IsNullOrEmpty(existingSubject))
        {
            return (existingSubject, existingHtml);
        }

        string subject = existingSubject ?? string.Empty;
        string htmlContent = existingHtml ?? string.Empty;

        if (!string.IsNullOrEmpty(htmlContent))
        {
            subject = string.IsNullOrEmpty(subject) ? ("PRN232_G9_AutoGradingTool Notification") : subject;
            return (subject, htmlContent);
        }

        switch (message.Template)
        {
            case NotificationTemplateEnums.Registration:
            {
                // TODO: Implement event registration template
                break;
            }
            default:
            {
                subject = string.IsNullOrEmpty(subject) ? ("PRN232_G9_AutoGradingTool Notification") : subject;
                htmlContent = string.IsNullOrEmpty(htmlContent) ? (message.Content ?? string.Empty) : htmlContent;
                break;
            }
        }

        return (subject, htmlContent);
    }

} 