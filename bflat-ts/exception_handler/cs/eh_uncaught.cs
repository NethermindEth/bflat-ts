// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// An exception nobody catches. The exit code pins which EH policy the image
// was built with:
//
//   default build   - the throw is dispatched, the search finds no handler,
//                     and the runtime fail-fasts: abort(), exit code 134
//   --remove-eh     - the throw never reaches dispatch; __wrap_RhpThrowEx
//                     exits the guest directly with 1
//
// Either way the line after the throw must never run.
using System;

class Program
{
    static int Main()
    {
        Console.WriteLine("eh: before throw");
        throw new InvalidOperationException("uncaught on purpose");
    }
}
