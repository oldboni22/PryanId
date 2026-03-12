using Microsoft.Extensions.Logging;

namespace Application;

public static partial class LoggerMessages
{
    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Error,
        Message = "Exception occured while adding client {clientId} with owner {userId}")
    ]
    public static partial void ClientCreationFailed(
        this ILogger logger, string clientId, Guid userId, Exception ex);
}