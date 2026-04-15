using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;
using System.Text.RegularExpressions;

namespace PRN232_G9_AutoGradingTool.Application.Common.Helpers;

public static class NotificationTemplateHelper
{
    private static string ProcessTemplate(string template, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(template) || data == null || !data.Any())
            return template;

        var result = template;

        // Replace placeholders like {{variableName}}
        var regex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.IgnoreCase);
        result = regex.Replace(result, match =>
        {
            var key = match.Groups[1].Value;
            if (data.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? string.Empty;
            }
            
            // Return empty string instead of keeping placeholder to avoid displaying {{placeholder}} to users
            return string.Empty;
        });

        return result;
    }

}
