// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Tests that programs containing double operations compile and run
// under nofp stubs.  All FP helpers (__adddf3, __subdf3, __muldf3,
// __divdf3, __floatsidf, __fixdfsi, …) are no-ops in the nofp module,
// so numerical results are undefined/zero.  The only thing we verify
// here is that execution reaches Main() and exits cleanly.
using System;

class Program
{
    // Non-const statics force the compiler to emit actual FP instructions
    // (or soft-float helper calls) at runtime instead of constant-folding.
    static double s_a = 1.0;
    static double s_b = 2.0;

    static int Main()
    {
        // Trigger __adddf3 stub (no-op under nofp) – result is undefined.
        double sum = s_a + s_b;

        // Trigger __muldf3 stub.
        double product = s_a * s_b;

        // Trigger __subdf3 stub.
        double diff = s_b - s_a;

        // Do NOT use the results in control flow: with nofp the values are
        // garbage/zero and comparing them would give wrong answers.
        // A side-effect reference prevents the compiler from eliminating the
        // computations as dead code.
        Console.WriteLine(
            $"nofp: double ops executed (sum/product/diff computed)");

        _ = sum;
        _ = product;
        _ = diff;

        return 0;
    }
}