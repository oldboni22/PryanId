namespace Shared;

public static class TimeProviderExtensions
{
    extension(TimeProvider timeProvider)
    {
        public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
    }    
}
