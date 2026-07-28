// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Pure-integer DateTime/TimeSpan arithmetic (ticks) must be exact; no
// double-based rounding members are involved.
using System;

class Program
{
    static int Main()
    {
        var dt = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
        DateTime later = dt + TimeSpan.FromTicks(TimeSpan.TicksPerHour * 5);
        if (later.Hour != 17) return 1;
        if ((later - dt).Ticks != TimeSpan.TicksPerHour * 5) return 2;
        DateTime nextDay = dt.AddTicks(TimeSpan.TicksPerDay);
        if (nextDay.Day != 28) return 3;
        if (nextDay.Month != 7) return 4;
        var ts = new TimeSpan(1, 2, 3, 4);
        if (ts.Ticks != TimeSpan.TicksPerDay + 2 * TimeSpan.TicksPerHour +
                        3 * TimeSpan.TicksPerMinute + 4 * TimeSpan.TicksPerSecond)
            return 5;
        Console.WriteLine("bcl_subst: datetime_add_timespan ok");
        return 0;
    }
}
