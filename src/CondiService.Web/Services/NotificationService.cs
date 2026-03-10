namespace CondiService.Web.Services;

public class NotificationService
{
    public const string TempDataKey = "Flash";

    public static string Success(string message) => "success|" + message;
    public static string Info(string message) => "info|" + message;
    public static string Warning(string message) => "warning|" + message;
    public static string Danger(string message) => "danger|" + message;

    public static (string type, string message)? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var idx = raw.IndexOf('|');
        if (idx <= 0)
            return ("info", raw);

        return (raw[..idx], raw[(idx + 1)..]);
    }
}
