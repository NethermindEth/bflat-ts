// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static void Show<T>(T[] xs) { Console.WriteLine(xs.Length); }

    static int Main(string[] args)
    {
        Show<string>(args);
        return 0;
    }
}
