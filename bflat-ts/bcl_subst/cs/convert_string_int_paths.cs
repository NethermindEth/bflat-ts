// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Convert.ToInt32/ToInt64 integer and string overloads stay fully
// functional while Convert.ToInt32(double) is removed.
using System;

class Program
{
    static int Main()
    {
        if (Convert.ToInt32("123") != 123) return 1;
        if (Convert.ToInt32("-456") != -456) return 2;
        if (Convert.ToInt64("9000000000") != 9000000000L) return 3;
        if (Convert.ToInt32(789L) != 789) return 4;
        if (Convert.ToInt32((short)-12) != -12) return 5;
        if (Convert.ToInt64(int.MaxValue) != 2147483647L) return 6;
        if (Convert.ToInt32(true) != 1) return 7;
        Console.WriteLine("bcl_subst: convert_string_int_paths ok");
        return 0;
    }
}
