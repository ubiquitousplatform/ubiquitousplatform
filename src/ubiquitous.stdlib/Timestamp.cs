namespace ubiquitous.stdlib;

/// <summary>Class to get current timestamp with enough precision</summary>
public static class Timestamp
{
    private static readonly DateTime Jan1St1970 = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    /// <summary>Get extra long current timestamp</summary>
    public static long UtcMs => (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds);
}