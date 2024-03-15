using System.Diagnostics;

namespace ubiquitous.stdlib;

public static class TimerExtensions
{
    private const double NanosecondsFactor = 1000000000.0;
    private const double MicrosecondsFactor = 1000000.0;

    public static long ElapsedNanoSeconds(this Stopwatch sw)
    {
        return (long)(NanosecondsFactor * sw.ElapsedTicks / Stopwatch.Frequency);
    }

    public static long ElapsedMicroseconds(this Stopwatch sw)
    {
        return (long)(MicrosecondsFactor * sw.ElapsedTicks / Stopwatch.Frequency);
    }
}