// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Tests that __wrap_getenv returns "1" for the .NET runtime environment
// variable names that the PAL module hard-codes, and returns null for
// unknown variables.

using System;

class Program
{
    static int Main()
    {
        // __wrap_getenv returns "1" for these well-known dotnet keys
        string inv = Environment.GetEnvironmentVariable(
            "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");
        if (inv != "1")
        {
            Console.WriteLine(
                $"pal: GLOBALIZATION_INVARIANT={inv ?? "null"} (expected 1)");
            return 1;
        }

        string predefined = Environment.GetEnvironmentVariable(
            "DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY");
        if (predefined != "1")
        {
            Console.WriteLine(
                $"pal: PREDEFINED_CULTURES_ONLY={predefined ?? "null"} (expected 1)");
            return 1;
        }

        string pool = Environment.GetEnvironmentVariable(
            "DOTNET_SYSTEM_BUFFERS_SHAREDARRAYPOOL_MAXPARTITIONCOUNT");
        if (pool != "1")
        {
            Console.WriteLine(
                $"pal: SHAREDARRAYPOOL_MAXPARTITIONCOUNT={pool ?? "null"} (expected 1)");
            return 1;
        }

        // Unknown variable must return null (__wrap_getenv returns 0)
        string unk = Environment.GetEnvironmentVariable("_BFLAT_TS_NO_SUCH_VAR_XYZ_");
        if (unk != null)
        {
            Console.WriteLine($"pal: unknown var unexpectedly returned '{unk}'");
            return 1;
        }

        Console.WriteLine("pal: env_dotnet ok");
        return 0;
    }
}