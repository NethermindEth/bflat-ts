// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Environment.SystemPageSize reads through the wrapped sysconf; it must be
// a positive power of two.
using System;

class Program
{
    static int Main()
    {
        int page = Environment.SystemPageSize;
        if (page <= 0) return 1;
        if ((page & (page - 1)) != 0) return 2;
        Console.WriteLine("bcl_subst: env_page_size ok");
        return 0;
    }
}
