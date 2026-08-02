// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class H {
    [ThreadStatic] public static int I;
    [ThreadStatic] public static long L;
    [ThreadStatic] public static string S;
    [ThreadStatic] public static bool B;
}
class Program
{
    static int Main()
    {
        H.I = 1; H.L = 2L; H.S = "x"; H.B = true;
        if (H.I != 1 || H.L != 2L || H.S != "x" || !H.B) return 1;
        Console.WriteLine("tls: mix_types ok");
        return 0;
    }
}
