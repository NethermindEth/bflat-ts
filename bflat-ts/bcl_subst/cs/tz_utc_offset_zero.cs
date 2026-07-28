// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// zisk runs with the invariant timezone; the transition-math members are
// stubbed (HasDaylightSaving=false, UtcOffsetOutOfRange=false). Local time
// must behave as UTC.
using System;

class Program
{
    static int Main()
    {
        if (TimeZoneInfo.Local.BaseUtcOffset.Ticks != 0) return 1;
        if (TimeZoneInfo.Local.SupportsDaylightSavingTime) return 2;
        DateTime now = DateTime.Now;
        DateTime utc = DateTime.UtcNow;
        long deltaTicks = (utc - now).Ticks;
        if (deltaTicks < 0) deltaTicks = -deltaTicks;
        if (deltaTicks > TimeSpan.TicksPerMinute) return 3;
        Console.WriteLine("bcl_subst: tz_utc_offset_zero ok");
        return 0;
    }
}
