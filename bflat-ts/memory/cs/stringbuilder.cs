// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Exercises: StringBuilder internal realloc on capacity doublings
// Bug targeted: same realloc heap bloat
using System;
using System.Text;

class Program
{
    static int Main()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < 400; i++)
            sb.Append("hello_");

        // Check total length: 6 chars * 400 appends = 2400
        if (sb.Length != 2400)
        {
            Console.WriteLine($"stringbuilder: FAIL length={sb.Length} expected=2400");
            return 1;
        }

        // Check first character of first chunk
        if (sb[0] != 'h')
        {
            Console.WriteLine($"stringbuilder: FAIL sb[0]='{sb[0]}' expected='h'");
            return 1;
        }

        // Check last character of last chunk
        if (sb[sb.Length - 1] != '_')
        {
            Console.WriteLine($"stringbuilder: FAIL sb[last]='{sb[sb.Length - 1]}' expected='_'");
            return 1;
        }

        // Check first character of second "hello_" (starts at index 6)
        if (sb[6] != 'h')
        {
            Console.WriteLine($"stringbuilder: FAIL sb[6]='{sb[6]}' expected='h'");
            return 1;
        }

        Console.WriteLine($"stringbuilder: ok length={sb.Length} first='{sb[0]}' last='{sb[sb.Length - 1]}' sb[6]='{sb[6]}'");
        return 0;
    }
}