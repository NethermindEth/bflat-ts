// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// An exception nobody catches. The exit code pins which EH policy the image
// was built with:
//
//   default build   - the throw is dispatched, the search walks every frame
//                     down to the reverse P/Invoke frame of Main, finds no
//                     handler, and the runtime fail-fasts: abort(), exit
//                     code 134
//   --remove-eh     - the throw never reaches dispatch; __wrap_RhpThrowEx
//                     exits the guest directly with 1
//
// Either way neither the finally nor the line after it runs: an unhandled
// exception fail-fasts after the first pass, without a second.
using System;
using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Inner(int n)
    {
        if (n == 0)
            throw new InvalidOperationException("uncaught on purpose");
        Inner(n - 1);
    }

    static int Main()
    {
        Console.WriteLine("eh: before throw");
        try
        {
            Inner(5);
        }
        finally
        {
            Console.WriteLine("eh: [not reached] finally");
        }
        Console.WriteLine("eh: [not reached] after");
        return 0;
    }
}
