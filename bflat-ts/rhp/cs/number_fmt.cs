// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Tests __wrap_UInt32ToDecStrForKnownSmallNumber by calling
// integer-to-string conversion for both small and larger numbers.
using System;

class Program
{
    static int Main()
    {
        // Small numbers (0-9): may go through KnownSmallNumber path
        for (uint i = 0; i <= 9; i++)
        {
            string s = i.ToString();
            if (s.Length != 1) return 1;
            if (s[0] != '0' + (char)i) return 1;
        }

        // Larger numbers
        if (42u.ToString() != "42") return 1;
        if (100u.ToString() != "100") return 1;
        if (1000u.ToString() != "1000") return 1;
        if (65535u.ToString() != "65535") return 1;

        Console.WriteLine("rhp: number_fmt ok");
        return 0;
    }
}