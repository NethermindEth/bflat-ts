// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// The custom-timezone API keeps working with the stubbed validation members
// (UtcOffsetOutOfRange=false, HasDaylightSaving=false) and the IL-patched
// TimeZoneInfo cctor (s_daylightRuleMarker built from integer ticks).
using System;

class Program
{
    static int Main()
    {
        var zero = TimeZoneInfo.CreateCustomTimeZone("tz0", TimeSpan.Zero,
                                                     "Zero Zone", "Zero Zone");
        var utc = new DateTime(2026, 1, 15, 6, 30, 0, DateTimeKind.Utc);
        DateTime asZero = TimeZoneInfo.ConvertTimeFromUtc(utc, zero);
        if ((asZero - utc).Ticks != 0) return 1;
        if (zero.SupportsDaylightSavingTime) return 2;

        var plus2 = TimeZoneInfo.CreateCustomTimeZone("tz2", new TimeSpan(2, 0, 0),
                                                      "Plus Two", "Plus Two");
        DateTime shifted = TimeZoneInfo.ConvertTimeFromUtc(utc, plus2);
        if ((shifted - utc).Ticks != 2 * TimeSpan.TicksPerHour) return 3;
        if (shifted.Hour != 8) return 4;
        Console.WriteLine("bcl_subst: tz_custom_zone_roundtrip ok");
        return 0;
    }
}
