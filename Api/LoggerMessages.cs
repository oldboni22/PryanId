namespace Api;

internal static partial class LoggerMessages
{
    [LoggerMessage(
        EventId = 500,
        Level = LogLevel.Error,
        Message = "Exception middleware caught an unhandled exception")
    ]
    public static partial void UnhandledException(
        this ILogger logger, Exception ex);
}
