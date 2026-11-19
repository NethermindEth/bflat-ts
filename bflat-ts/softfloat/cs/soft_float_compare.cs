// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// float and double comparisons on a target without the F and D extensions.
//
// There is no floating-point compare instruction. The compiler calls one of
// two three-way routines per type (__ledf2 and __gedf2, __lesf2 and __gesf2)
// and compares the result with zero. The two routines agree for ordered
// operands and differ when one operand is a NaN: the first then returns 1,
// the second -1. Which routine is called and which integer comparison
// follows depends on the comparison and on whether it has to hold or to fail
// for a NaN, so there are twelve cases per type. What can go wrong: the
// wrong routine or the wrong integer comparison for one of the cases, which
// shows only with a NaN operand; a comparison negated by the compiler as if
// !(a < b) were a >= b; +0 and -0 told apart; a float compared with the
// double routine; a comparison of a value with itself or with a constant
// decided at compile time against the rules; and a result that the compiler
// computes itself from constant operands and that differs from the routine.
//
// Twelve values per type: two NaNs, +0, -0, 1, -1, the value after 1, the
// smallest subnormal of each sign, the largest finite, and the infinities.
// For every pair the six comparisons are made as values, as branches, as
// negated branches, stored in locals, as loop conditions, joined with & and
// && and ||, and as the condition that selects one of the two operands.
// Every value is also compared with itself and with constants on either
// side, classified with IsNaN and its relatives, and put through a chain of
// comparisons. A part of the pairs is repeated with both operands constant.
// Mixed pairs compare a float with a double. Two arrays are sorted with >.
//
// The expected results are tables of bit masks (1 <, 2 <=, 4 >, 8 >=,
// 16 ==, 32 !=) computed outside the test from the bit patterns. Each failed
// check prints one line; the run ends with ALL PASS or a FAIL count, and the
// guest returns 0 or 1.
using System;
using System.Runtime.CompilerServices;

unsafe class Program
{
    const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
    const MethodImplOptions Inl = MethodImplOptions.AggressiveInlining;

    static int fails;
    static int checks;

    [MethodImpl(NoInl)]
    static void CheckI(string name, int expected, int got)
    {
        checks++;
        if (got == expected) return;
        fails++;
        Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
    }

    [MethodImpl(NoInl)]
    static void CheckD(string name, ulong expected, double got)
    {
        ulong g = *(ulong*)&got;
        checks++;
        if (g == expected) return;
        fails++;
        Console.WriteLine("FAIL " + name + " expected 0x" + expected.ToString("x16") + " got 0x" + g.ToString("x16"));
    }

    [MethodImpl(NoInl)]
    static void CheckF(string name, uint expected, float got)
    {
        uint g = *(uint*)&got;
        checks++;
        if (g == expected) return;
        fails++;
        Console.WriteLine("FAIL " + name + " expected 0x" + expected.ToString("x8") + " got 0x" + g.ToString("x8"));
    }

    [MethodImpl(Inl)] static int B(bool b) => b ? 1 : 0;
    [MethodImpl(NoInl)] static void Touch(bool* p) { }

    // A constant the compiler can see.
    [MethodImpl(Inl)] static double CD(ulong bits) => BitConverter.UInt64BitsToDouble(bits);
    [MethodImpl(Inl)] static float CF(uint bits) => BitConverter.UInt32BitsToSingle(bits);

    static readonly ulong[] ValsD =
    {
        0x7ff8000000000000UL, 0xfff8000000000001UL, 0x0000000000000000UL, 0x8000000000000000UL,
        0x3ff0000000000000UL, 0xbff0000000000000UL, 0x3ff0000000000001UL, 0x0000000000000001UL,
        0x8000000000000001UL, 0x7fefffffffffffffUL, 0x7ff0000000000000UL, 0xfff0000000000000UL,
    };

    static readonly uint[] ValsF =
    {
        0x7fc00000U, 0xffc00001U, 0x00000000U, 0x80000000U, 0x3f800000U, 0xbf800000U,
        0x3f800001U, 0x00000001U, 0x80000001U, 0x7f7fffffU, 0x7f800000U, 0xff800000U,
    };

    static readonly byte[] MaskD =
    {
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 26, 26, 35, 44, 35, 35, 44, 35, 35, 44,
        32, 32, 26, 26, 35, 44, 35, 35, 44, 35, 35, 44,
        32, 32, 44, 44, 26, 44, 35, 44, 44, 35, 35, 44,
        32, 32, 35, 35, 35, 26, 35, 35, 35, 35, 35, 44,
        32, 32, 44, 44, 44, 44, 26, 44, 44, 35, 35, 44,
        32, 32, 44, 44, 35, 44, 35, 26, 44, 35, 35, 44,
        32, 32, 35, 35, 35, 44, 35, 35, 26, 35, 35, 44,
        32, 32, 44, 44, 44, 44, 44, 44, 44, 26, 35, 44,
        32, 32, 44, 44, 44, 44, 44, 44, 44, 44, 26, 44,
        32, 32, 35, 35, 35, 35, 35, 35, 35, 35, 35, 26,
    };

    static readonly byte[] MaskF =
    {
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 26, 26, 35, 44, 35, 35, 44, 35, 35, 44,
        32, 32, 26, 26, 35, 44, 35, 35, 44, 35, 35, 44,
        32, 32, 44, 44, 26, 44, 35, 44, 44, 35, 35, 44,
        32, 32, 35, 35, 35, 26, 35, 35, 35, 35, 35, 44,
        32, 32, 44, 44, 44, 44, 26, 44, 44, 35, 35, 44,
        32, 32, 44, 44, 35, 44, 35, 26, 44, 35, 35, 44,
        32, 32, 35, 35, 35, 44, 35, 35, 26, 35, 35, 44,
        32, 32, 44, 44, 44, 44, 44, 44, 44, 26, 35, 44,
        32, 32, 44, 44, 44, 44, 44, 44, 44, 44, 26, 44,
        32, 32, 35, 35, 35, 35, 35, 35, 35, 35, 35, 26,
    };

    static readonly byte[] MaskFD =
    {
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 26, 26, 35, 44, 35, 35, 44, 35, 35, 44,
        32, 32, 26, 26, 35, 44, 35, 35, 44, 35, 35, 44,
        32, 32, 44, 44, 26, 44, 35, 44, 44, 35, 35, 44,
        32, 32, 35, 35, 35, 26, 35, 35, 35, 35, 35, 44,
        32, 32, 44, 44, 44, 44, 44, 44, 44, 35, 35, 44,
        32, 32, 44, 44, 35, 44, 35, 44, 44, 35, 35, 44,
        32, 32, 35, 35, 35, 44, 35, 35, 35, 35, 35, 44,
        32, 32, 44, 44, 44, 44, 44, 44, 44, 35, 35, 44,
        32, 32, 44, 44, 44, 44, 44, 44, 44, 44, 26, 44,
        32, 32, 35, 35, 35, 35, 35, 35, 35, 35, 35, 26,
    };

    static readonly byte[] MaskDF =
    {
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
        32, 32, 26, 26, 35, 44, 35, 35, 44, 35, 35, 44,
        32, 32, 26, 26, 35, 44, 35, 35, 44, 35, 35, 44,
        32, 32, 44, 44, 26, 44, 35, 44, 44, 35, 35, 44,
        32, 32, 35, 35, 35, 26, 35, 35, 35, 35, 35, 44,
        32, 32, 44, 44, 44, 44, 35, 44, 44, 35, 35, 44,
        32, 32, 44, 44, 35, 44, 35, 35, 44, 35, 35, 44,
        32, 32, 35, 35, 35, 44, 35, 35, 44, 35, 35, 44,
        32, 32, 44, 44, 44, 44, 44, 44, 44, 44, 35, 44,
        32, 32, 44, 44, 44, 44, 44, 44, 44, 44, 26, 44,
        32, 32, 35, 35, 35, 35, 35, 35, 35, 35, 35, 26,
    };

    static readonly int[] ClassTabD = { 1, 9, 4, 12, 20, 28, 20, 36, 44, 20, 66, 138 };
    static readonly int[] ClassTabF = { 1, 9, 4, 12, 20, 28, 20, 36, 44, 20, 66, 138 };

    static readonly ulong[] SortInD =
    {
        0x400c000000000000UL, 0xbff0000000000000UL, 0x0000000000000000UL, 0x7e37e43c8800759cUL,
        0x8000000000000000UL, 0x4004000000000000UL, 0x80000000000007e8UL, 0x00000000000007e8UL,
        0x401c000000000000UL, 0xc01c000000000000UL, 0x3fb999999999999aUL, 0x3fd3333333333334UL,
        0x3fd3333333333333UL, 0x7ff0000000000000UL, 0xfff0000000000000UL, 0x3ff0000000000001UL,
        0x3ff0000000000000UL, 0x3fefffffffffffffUL,
    };
    static readonly ulong[] SortOutD =
    {
        0xfff0000000000000UL, 0xc01c000000000000UL, 0xbff0000000000000UL, 0x80000000000007e8UL,
        0x0000000000000000UL, 0x8000000000000000UL, 0x00000000000007e8UL, 0x3fb999999999999aUL,
        0x3fd3333333333333UL, 0x3fd3333333333334UL, 0x3fefffffffffffffUL, 0x3ff0000000000000UL,
        0x3ff0000000000001UL, 0x4004000000000000UL, 0x400c000000000000UL, 0x401c000000000000UL,
        0x7e37e43c8800759cUL, 0x7ff0000000000000UL,
    };

    static readonly uint[] SortInF =
    {
        0x40600000U, 0xbf800000U, 0x00000000U, 0x7149f2caU, 0x80000000U, 0x40200000U,
        0x40e00000U, 0xc0e00000U, 0x3dcccccdU, 0x3e99999aU, 0x80000003U, 0x00000003U,
        0x7f800000U, 0xff800000U, 0x3f800001U, 0x3f800000U, 0x3f7fffffU, 0x3e99999bU,
    };
    static readonly uint[] SortOutF =
    {
        0xff800000U, 0xc0e00000U, 0xbf800000U, 0x80000003U, 0x00000000U, 0x80000000U,
        0x00000003U, 0x3dcccccdU, 0x3e99999aU, 0x3e99999bU, 0x3f7fffffU, 0x3f800000U,
        0x3f800001U, 0x40200000U, 0x40600000U, 0x40e00000U, 0x7149f2caU, 0x7f800000U,
    };

    static readonly int[] BucketTabD = { 9, 9, 2, 2, 3, 1, 3, 2, 1, 4, 4, 0 };
    static readonly int[] BucketTabF = { 9, 9, 2, 2, 3, 1, 3, 2, 1, 4, 4, 0 };


    // ---- double ----

    [MethodImpl(NoInl)] static bool LtD(double a, double b) => a < b;
    [MethodImpl(NoInl)] static bool LeD(double a, double b) => a <= b;
    [MethodImpl(NoInl)] static bool GtD(double a, double b) => a > b;
    [MethodImpl(NoInl)] static bool GeD(double a, double b) => a >= b;
    [MethodImpl(NoInl)] static bool EqD(double a, double b) => a == b;
    [MethodImpl(NoInl)] static bool NeD(double a, double b) => a != b;

    // The result of each comparison as a value.
    [MethodImpl(NoInl)]
    static int ValueD(double a, double b)
        => B(LtD(a, b)) | B(LeD(a, b)) << 1 | B(GtD(a, b)) << 2 | B(GeD(a, b)) << 3
           | B(EqD(a, b)) << 4 | B(NeD(a, b)) << 5;

    // Each comparison as a branch that is taken when it holds.
    [MethodImpl(Inl)]
    static int InlD(double a, double b)
    {
        int m = 0;
        if (a < b) m |= 1;
        if (a <= b) m |= 2;
        if (a > b) m |= 4;
        if (a >= b) m |= 8;
        if (a == b) m |= 16;
        if (a != b) m |= 32;
        return m;
    }

    [MethodImpl(NoInl)] static int BranchD(double a, double b) => InlD(a, b);

    // Each comparison negated: a branch that is taken when it does not hold,
    // which for a NaN is not the opposite comparison.
    [MethodImpl(NoInl)]
    static int NotBranchD(double a, double b)
    {
        int m = 63;
        if (!(a < b)) m &= ~1;
        if (!(a <= b)) m &= ~2;
        if (!(a > b)) m &= ~4;
        if (!(a >= b)) m &= ~8;
        if (!(a == b)) m &= ~16;
        if (!(a != b)) m &= ~32;
        return m;
    }

    // Results kept in locals and read back as bytes.
    [MethodImpl(NoInl)]
    static int StoredD(double a, double b)
    {
        bool lt = a < b, le = a <= b, gt = a > b, ge = a >= b, eq = a == b, ne = a != b;
        Touch(&lt); Touch(&le); Touch(&gt); Touch(&ge); Touch(&eq); Touch(&ne);
        return *(byte*)&lt | *(byte*)&le << 1 | *(byte*)&gt << 2 | *(byte*)&ge << 3 | *(byte*)&eq << 4
               | *(byte*)&ne << 5;
    }

    // Comparisons as loop conditions, joined with an integer condition.
    [MethodImpl(NoInl)]
    static int LoopD(double a, double b, int n)
    {
        int m = 0;
        for (int i = 0; i < n && a < b; i++) m |= 1;
        for (int i = 0; a <= b && i < n; i++) m |= 2;
        int k = n;
        while (a > b) { m |= 4; if (--k <= 0) break; }
        k = n;
        do { if (!(a >= b)) break; m |= 8; } while (--k > 0);
        for (int i = 0; i < n; i++) { if (a == b) m |= 16; else m |= 64; }
        for (int i = 0; i < n; i++) { if (a != b) { m |= 32; continue; } m |= 128; }
        return m;
    }

    // A comparison that selects one of two floating-point values.
    [MethodImpl(NoInl)]
    static double SelectD(double a, double b, int op)
    {
        switch (op)
        {
            case 0: return a < b ? a : b;
            case 1: return a <= b ? a : b;
            case 2: return a > b ? a : b;
            case 3: return a >= b ? a : b;
            case 4: return a == b ? a : b;
            default: return a != b ? a : b;
        }
    }

    // Comparisons joined without branches and with short-circuit operators.
    [MethodImpl(NoInl)]
    static int JoinedD(double a, double b)
    {
        int m = 0;
        if ((a < b) | (a == b)) m |= 1;         // <= for ordered operands
        if (a < b || a > b) m |= 2;             // ordered and not equal
        if (!(a < b) && !(a > b)) m |= 4;       // equal or unordered
        if ((a <= b) & (a >= b)) m |= 8;        // ==
        if (!(a <= b) & !(a >= b)) m |= 16;     // unordered
        if ((a == b) == (b == a) && (a < b) == (b > a) && (a <= b) == (b >= a)) m |= 32;
        return m;
    }

    // A value compared with itself.
    [MethodImpl(NoInl)]
    static int SelfD(double x)
    {
        int m = 0;
#pragma warning disable CS1718
        if (x < x) m |= 1;
        if (x <= x) m |= 2;
        if (x > x) m |= 4;
        if (x >= x) m |= 8;
        if (x == x) m |= 16;
        if (x != x) m |= 32;
#pragma warning restore CS1718
        return m;
    }

    // One operand is a constant: +0, -0, 1, NaN, +infinity (rows 2, 3, 4, 0, 10).
    [MethodImpl(NoInl)] static int ConstR0D(double x) => InlD(x, 0.0);
    [MethodImpl(NoInl)] static int ConstR1D(double x) => InlD(x, -0.0);
    [MethodImpl(NoInl)] static int ConstR2D(double x) => InlD(x, 1.0);
    [MethodImpl(NoInl)] static int ConstR3D(double x) => InlD(x, double.NaN);
    [MethodImpl(NoInl)] static int ConstR4D(double x) => InlD(x, double.PositiveInfinity);
    [MethodImpl(NoInl)] static int ConstL0D(double x) => InlD(0.0, x);
    [MethodImpl(NoInl)] static int ConstL1D(double x) => InlD(-0.0, x);
    [MethodImpl(NoInl)] static int ConstL2D(double x) => InlD(1.0, x);
    [MethodImpl(NoInl)] static int ConstL3D(double x) => InlD(double.NaN, x);
    [MethodImpl(NoInl)] static int ConstL4D(double x) => InlD(double.PositiveInfinity, x);

    [MethodImpl(NoInl)]
    static int ClassD(double x)
        => B(double.IsNaN(x)) | B(double.IsInfinity(x)) << 1 | B(double.IsFinite(x)) << 2 | B(double.IsNegative(x)) << 3
           | B(double.IsNormal(x)) << 4 | B(double.IsSubnormal(x)) << 5 | B(double.IsPositiveInfinity(x)) << 6
           | B(double.IsNegativeInfinity(x)) << 7;

    [MethodImpl(NoInl)]
    static int BucketD(double x)
    {
        if (x != x) return 9;
        if (x < -1.0) return 0;
        else if (x < 0.0) return 1;
        else if (x < 1.0) return 2;
        else if (x < 2.0) return 3;
        return 4;
    }

    [MethodImpl(NoInl)]
    static void SortD(double* a, int n)
    {
        for (int i = 1; i < n; i++)
        {
            double x = a[i];
            int j = i - 1;
            while (j >= 0 && a[j] > x) { a[j + 1] = a[j]; j--; }
            a[j + 1] = x;
        }
    }

    static void RunD()
    {
        const int N = 12;
        fixed (ulong* v = ValsD)
        {
            for (int i = 0; i < N; i++)
            {
                double a = *(double*)(v + i);
                string ri = "double row " + i;
                CheckI(ri + " with itself", MaskD[i * N + i], SelfD(a));
                CheckI(ri + " classification", ClassTabD[i], ClassD(a));
                CheckI(ri + " bucket", BucketTabD[i], BucketD(a));
                CheckI(ri + " with +0 on the right", MaskD[i * N + 2], ConstR0D(a));
                CheckI(ri + " with -0 on the right", MaskD[i * N + 3], ConstR1D(a));
                CheckI(ri + " with 1 on the right", MaskD[i * N + 4], ConstR2D(a));
                CheckI(ri + " with NaN on the right", MaskD[i * N + 0], ConstR3D(a));
                CheckI(ri + " with +infinity on the right", MaskD[i * N + 10], ConstR4D(a));
                CheckI(ri + " with +0 on the left", MaskD[2 * N + i], ConstL0D(a));
                CheckI(ri + " with -0 on the left", MaskD[3 * N + i], ConstL1D(a));
                CheckI(ri + " with 1 on the left", MaskD[4 * N + i], ConstL2D(a));
                CheckI(ri + " with NaN on the left", MaskD[0 * N + i], ConstL3D(a));
                CheckI(ri + " with +infinity on the left", MaskD[10 * N + i], ConstL4D(a));
                for (int j = 0; j < N; j++)
                {
                    double b = *(double*)(v + j);
                    int e = MaskD[i * N + j];
                    string rij = "double rows " + i + " and " + j;
                    CheckI(rij + " as values", e, ValueD(a, b));
                    CheckI(rij + " as branches", e, BranchD(a, b));
                    CheckI(rij + " as negated branches", e, NotBranchD(a, b));
                    CheckI(rij + " stored", e, StoredD(a, b));
                    // Loop adds 64 where == fails and 128 where != fails.
                    CheckI(rij + " as loop conditions", e | ((e & 16) == 0 ? 64 : 0) | ((e & 32) == 0 ? 128 : 0),
                           LoopD(a, b, 3));
                    bool unordered = (e & (1 | 4 | 16)) == 0;
                    int joined = ((e & 2) != 0 ? 1 : 0) | ((e & (1 | 4)) != 0 ? 2 : 0) | ((e & (1 | 4)) == 0 ? 4 : 0)
                                 | ((e & 16) != 0 ? 8 : 0) | (unordered ? 16 : 0) | 32;
                    CheckI(rij + " joined", joined, JoinedD(a, b));
                    for (int op = 0; op < 6; op++)
                    {
                        double s = SelectD(a, b, op);
                        CheckD(rij + " selecting by comparison " + op, (e >> op & 1) != 0 ? v[i] : v[j], s);
                    }
                }
            }
        }

        int n = SortInD.Length;
        double* w = stackalloc double[n];
        for (int i = 0; i < n; i++) *(ulong*)(w + i) = SortInD[i];
        SortD(w, n);
        for (int i = 0; i < n; i++) CheckD("double sorted, element " + i, SortOutD[i], w[i]);
    }


    // ---- float ----

    [MethodImpl(NoInl)] static bool LtF(float a, float b) => a < b;
    [MethodImpl(NoInl)] static bool LeF(float a, float b) => a <= b;
    [MethodImpl(NoInl)] static bool GtF(float a, float b) => a > b;
    [MethodImpl(NoInl)] static bool GeF(float a, float b) => a >= b;
    [MethodImpl(NoInl)] static bool EqF(float a, float b) => a == b;
    [MethodImpl(NoInl)] static bool NeF(float a, float b) => a != b;

    // The result of each comparison as a value.
    [MethodImpl(NoInl)]
    static int ValueF(float a, float b)
        => B(LtF(a, b)) | B(LeF(a, b)) << 1 | B(GtF(a, b)) << 2 | B(GeF(a, b)) << 3
           | B(EqF(a, b)) << 4 | B(NeF(a, b)) << 5;

    // Each comparison as a branch that is taken when it holds.
    [MethodImpl(Inl)]
    static int InlF(float a, float b)
    {
        int m = 0;
        if (a < b) m |= 1;
        if (a <= b) m |= 2;
        if (a > b) m |= 4;
        if (a >= b) m |= 8;
        if (a == b) m |= 16;
        if (a != b) m |= 32;
        return m;
    }

    [MethodImpl(NoInl)] static int BranchF(float a, float b) => InlF(a, b);

    // Each comparison negated: a branch that is taken when it does not hold,
    // which for a NaN is not the opposite comparison.
    [MethodImpl(NoInl)]
    static int NotBranchF(float a, float b)
    {
        int m = 63;
        if (!(a < b)) m &= ~1;
        if (!(a <= b)) m &= ~2;
        if (!(a > b)) m &= ~4;
        if (!(a >= b)) m &= ~8;
        if (!(a == b)) m &= ~16;
        if (!(a != b)) m &= ~32;
        return m;
    }

    // Results kept in locals and read back as bytes.
    [MethodImpl(NoInl)]
    static int StoredF(float a, float b)
    {
        bool lt = a < b, le = a <= b, gt = a > b, ge = a >= b, eq = a == b, ne = a != b;
        Touch(&lt); Touch(&le); Touch(&gt); Touch(&ge); Touch(&eq); Touch(&ne);
        return *(byte*)&lt | *(byte*)&le << 1 | *(byte*)&gt << 2 | *(byte*)&ge << 3 | *(byte*)&eq << 4
               | *(byte*)&ne << 5;
    }

    // Comparisons as loop conditions, joined with an integer condition.
    [MethodImpl(NoInl)]
    static int LoopF(float a, float b, int n)
    {
        int m = 0;
        for (int i = 0; i < n && a < b; i++) m |= 1;
        for (int i = 0; a <= b && i < n; i++) m |= 2;
        int k = n;
        while (a > b) { m |= 4; if (--k <= 0) break; }
        k = n;
        do { if (!(a >= b)) break; m |= 8; } while (--k > 0);
        for (int i = 0; i < n; i++) { if (a == b) m |= 16; else m |= 64; }
        for (int i = 0; i < n; i++) { if (a != b) { m |= 32; continue; } m |= 128; }
        return m;
    }

    // A comparison that selects one of two floating-point values.
    [MethodImpl(NoInl)]
    static float SelectF(float a, float b, int op)
    {
        switch (op)
        {
            case 0: return a < b ? a : b;
            case 1: return a <= b ? a : b;
            case 2: return a > b ? a : b;
            case 3: return a >= b ? a : b;
            case 4: return a == b ? a : b;
            default: return a != b ? a : b;
        }
    }

    // Comparisons joined without branches and with short-circuit operators.
    [MethodImpl(NoInl)]
    static int JoinedF(float a, float b)
    {
        int m = 0;
        if ((a < b) | (a == b)) m |= 1;         // <= for ordered operands
        if (a < b || a > b) m |= 2;             // ordered and not equal
        if (!(a < b) && !(a > b)) m |= 4;       // equal or unordered
        if ((a <= b) & (a >= b)) m |= 8;        // ==
        if (!(a <= b) & !(a >= b)) m |= 16;     // unordered
        if ((a == b) == (b == a) && (a < b) == (b > a) && (a <= b) == (b >= a)) m |= 32;
        return m;
    }

    // A value compared with itself.
    [MethodImpl(NoInl)]
    static int SelfF(float x)
    {
        int m = 0;
#pragma warning disable CS1718
        if (x < x) m |= 1;
        if (x <= x) m |= 2;
        if (x > x) m |= 4;
        if (x >= x) m |= 8;
        if (x == x) m |= 16;
        if (x != x) m |= 32;
#pragma warning restore CS1718
        return m;
    }

    // One operand is a constant: +0, -0, 1, NaN, +infinity (rows 2, 3, 4, 0, 10).
    [MethodImpl(NoInl)] static int ConstR0F(float x) => InlF(x, 0.0f);
    [MethodImpl(NoInl)] static int ConstR1F(float x) => InlF(x, -0.0f);
    [MethodImpl(NoInl)] static int ConstR2F(float x) => InlF(x, 1.0f);
    [MethodImpl(NoInl)] static int ConstR3F(float x) => InlF(x, float.NaN);
    [MethodImpl(NoInl)] static int ConstR4F(float x) => InlF(x, float.PositiveInfinity);
    [MethodImpl(NoInl)] static int ConstL0F(float x) => InlF(0.0f, x);
    [MethodImpl(NoInl)] static int ConstL1F(float x) => InlF(-0.0f, x);
    [MethodImpl(NoInl)] static int ConstL2F(float x) => InlF(1.0f, x);
    [MethodImpl(NoInl)] static int ConstL3F(float x) => InlF(float.NaN, x);
    [MethodImpl(NoInl)] static int ConstL4F(float x) => InlF(float.PositiveInfinity, x);

    [MethodImpl(NoInl)]
    static int ClassF(float x)
        => B(float.IsNaN(x)) | B(float.IsInfinity(x)) << 1 | B(float.IsFinite(x)) << 2 | B(float.IsNegative(x)) << 3
           | B(float.IsNormal(x)) << 4 | B(float.IsSubnormal(x)) << 5 | B(float.IsPositiveInfinity(x)) << 6
           | B(float.IsNegativeInfinity(x)) << 7;

    [MethodImpl(NoInl)]
    static int BucketF(float x)
    {
        if (x != x) return 9;
        if (x < -1.0f) return 0;
        else if (x < 0.0f) return 1;
        else if (x < 1.0f) return 2;
        else if (x < 2.0f) return 3;
        return 4;
    }

    [MethodImpl(NoInl)]
    static void SortF(float* a, int n)
    {
        for (int i = 1; i < n; i++)
        {
            float x = a[i];
            int j = i - 1;
            while (j >= 0 && a[j] > x) { a[j + 1] = a[j]; j--; }
            a[j + 1] = x;
        }
    }

    static void RunF()
    {
        const int N = 12;
        fixed (uint* v = ValsF)
        {
            for (int i = 0; i < N; i++)
            {
                float a = *(float*)(v + i);
                string ri = "float row " + i;
                CheckI(ri + " with itself", MaskF[i * N + i], SelfF(a));
                CheckI(ri + " classification", ClassTabF[i], ClassF(a));
                CheckI(ri + " bucket", BucketTabF[i], BucketF(a));
                CheckI(ri + " with +0 on the right", MaskF[i * N + 2], ConstR0F(a));
                CheckI(ri + " with -0 on the right", MaskF[i * N + 3], ConstR1F(a));
                CheckI(ri + " with 1 on the right", MaskF[i * N + 4], ConstR2F(a));
                CheckI(ri + " with NaN on the right", MaskF[i * N + 0], ConstR3F(a));
                CheckI(ri + " with +infinity on the right", MaskF[i * N + 10], ConstR4F(a));
                CheckI(ri + " with +0 on the left", MaskF[2 * N + i], ConstL0F(a));
                CheckI(ri + " with -0 on the left", MaskF[3 * N + i], ConstL1F(a));
                CheckI(ri + " with 1 on the left", MaskF[4 * N + i], ConstL2F(a));
                CheckI(ri + " with NaN on the left", MaskF[0 * N + i], ConstL3F(a));
                CheckI(ri + " with +infinity on the left", MaskF[10 * N + i], ConstL4F(a));
                for (int j = 0; j < N; j++)
                {
                    float b = *(float*)(v + j);
                    int e = MaskF[i * N + j];
                    string rij = "float rows " + i + " and " + j;
                    CheckI(rij + " as values", e, ValueF(a, b));
                    CheckI(rij + " as branches", e, BranchF(a, b));
                    CheckI(rij + " as negated branches", e, NotBranchF(a, b));
                    CheckI(rij + " stored", e, StoredF(a, b));
                    // Loop adds 64 where == fails and 128 where != fails.
                    CheckI(rij + " as loop conditions", e | ((e & 16) == 0 ? 64 : 0) | ((e & 32) == 0 ? 128 : 0),
                           LoopF(a, b, 3));
                    bool unordered = (e & (1 | 4 | 16)) == 0;
                    int joined = ((e & 2) != 0 ? 1 : 0) | ((e & (1 | 4)) != 0 ? 2 : 0) | ((e & (1 | 4)) == 0 ? 4 : 0)
                                 | ((e & 16) != 0 ? 8 : 0) | (unordered ? 16 : 0) | 32;
                    CheckI(rij + " joined", joined, JoinedF(a, b));
                    for (int op = 0; op < 6; op++)
                    {
                        float s = SelectF(a, b, op);
                        CheckF(rij + " selecting by comparison " + op, (e >> op & 1) != 0 ? v[i] : v[j], s);
                    }
                }
            }
        }

        int n = SortInF.Length;
        float* w = stackalloc float[n];
        for (int i = 0; i < n; i++) *(uint*)(w + i) = SortInF[i];
        SortF(w, n);
        for (int i = 0; i < n; i++) CheckF("float sorted, element " + i, SortOutF[i], w[i]);
    }

    // ---- a float with a double ----

    [MethodImpl(NoInl)] static int MixedFD(float a, double b) => InlD(a, b);
    [MethodImpl(NoInl)] static int MixedDF(double a, float b) => InlD(a, b);

    static void RunMixed()
    {
        const int N = 12;
        fixed (ulong* vd = ValsD)
        fixed (uint* vf = ValsF)
        {
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    CheckI("float row " + i + " with double row " + j, MaskFD[i * N + j],
                           MixedFD(*(float*)(vf + i), *(double*)(vd + j)));
                    CheckI("double row " + i + " with float row " + j, MaskDF[i * N + j],
                           MixedDF(*(double*)(vd + i), *(float*)(vf + j)));
                }
            }
        }
    }

    // ---- both operands constant ----

    [MethodImpl(NoInl)]
    static void Folded()
    {
        CheckI("double constants 0 0", 32, InlD(CD(0x7ff8000000000000UL), CD(0x7ff8000000000000UL)));
        CheckI("float constants 0 1", 32, InlF(CF(0x7fc00000U), CF(0xffc00001U)));
        CheckI("double constants 0 3", 32, InlD(CD(0x7ff8000000000000UL), CD(0x8000000000000000UL)));
        CheckI("float constants 0 4", 32, InlF(CF(0x7fc00000U), CF(0x3f800000U)));
        CheckI("double constants 0 6", 32, InlD(CD(0x7ff8000000000000UL), CD(0x3ff0000000000001UL)));
        CheckI("float constants 0 7", 32, InlF(CF(0x7fc00000U), CF(0x00000001U)));
        CheckI("double constants 0 9", 32, InlD(CD(0x7ff8000000000000UL), CD(0x7fefffffffffffffUL)));
        CheckI("float constants 0 10", 32, InlF(CF(0x7fc00000U), CF(0x7f800000U)));
        CheckI("double constants 1 1", 32, InlD(CD(0xfff8000000000001UL), CD(0xfff8000000000001UL)));
        CheckI("float constants 1 2", 32, InlF(CF(0xffc00001U), CF(0x00000000U)));
        CheckI("double constants 1 4", 32, InlD(CD(0xfff8000000000001UL), CD(0x3ff0000000000000UL)));
        CheckI("float constants 1 5", 32, InlF(CF(0xffc00001U), CF(0xbf800000U)));
        CheckI("double constants 1 7", 32, InlD(CD(0xfff8000000000001UL), CD(0x0000000000000001UL)));
        CheckI("float constants 1 8", 32, InlF(CF(0xffc00001U), CF(0x80000001U)));
        CheckI("double constants 1 10", 32, InlD(CD(0xfff8000000000001UL), CD(0x7ff0000000000000UL)));
        CheckI("float constants 1 11", 32, InlF(CF(0xffc00001U), CF(0xff800000U)));
        CheckI("float constants 2 0", 32, InlF(CF(0x00000000U), CF(0x7fc00000U)));
        CheckI("double constants 2 2", 26, InlD(CD(0x0000000000000000UL), CD(0x0000000000000000UL)));
        CheckI("float constants 2 3", 26, InlF(CF(0x00000000U), CF(0x80000000U)));
        CheckI("double constants 2 5", 44, InlD(CD(0x0000000000000000UL), CD(0xbff0000000000000UL)));
        CheckI("float constants 2 6", 35, InlF(CF(0x00000000U), CF(0x3f800001U)));
        CheckI("double constants 2 8", 44, InlD(CD(0x0000000000000000UL), CD(0x8000000000000001UL)));
        CheckI("float constants 2 9", 35, InlF(CF(0x00000000U), CF(0x7f7fffffU)));
        CheckI("double constants 2 11", 44, InlD(CD(0x0000000000000000UL), CD(0xfff0000000000000UL)));
        CheckI("double constants 3 0", 32, InlD(CD(0x8000000000000000UL), CD(0x7ff8000000000000UL)));
        CheckI("float constants 3 1", 32, InlF(CF(0x80000000U), CF(0xffc00001U)));
        CheckI("double constants 3 3", 26, InlD(CD(0x8000000000000000UL), CD(0x8000000000000000UL)));
        CheckI("float constants 3 4", 35, InlF(CF(0x80000000U), CF(0x3f800000U)));
        CheckI("double constants 3 6", 35, InlD(CD(0x8000000000000000UL), CD(0x3ff0000000000001UL)));
        CheckI("float constants 3 7", 35, InlF(CF(0x80000000U), CF(0x00000001U)));
        CheckI("double constants 3 9", 35, InlD(CD(0x8000000000000000UL), CD(0x7fefffffffffffffUL)));
        CheckI("float constants 3 10", 35, InlF(CF(0x80000000U), CF(0x7f800000U)));
        CheckI("double constants 4 1", 32, InlD(CD(0x3ff0000000000000UL), CD(0xfff8000000000001UL)));
        CheckI("float constants 4 2", 44, InlF(CF(0x3f800000U), CF(0x00000000U)));
        CheckI("double constants 4 4", 26, InlD(CD(0x3ff0000000000000UL), CD(0x3ff0000000000000UL)));
        CheckI("float constants 4 5", 44, InlF(CF(0x3f800000U), CF(0xbf800000U)));
        CheckI("double constants 4 7", 44, InlD(CD(0x3ff0000000000000UL), CD(0x0000000000000001UL)));
        CheckI("float constants 4 8", 44, InlF(CF(0x3f800000U), CF(0x80000001U)));
        CheckI("double constants 4 10", 35, InlD(CD(0x3ff0000000000000UL), CD(0x7ff0000000000000UL)));
        CheckI("float constants 4 11", 44, InlF(CF(0x3f800000U), CF(0xff800000U)));
        CheckI("float constants 5 0", 32, InlF(CF(0xbf800000U), CF(0x7fc00000U)));
        CheckI("double constants 5 2", 35, InlD(CD(0xbff0000000000000UL), CD(0x0000000000000000UL)));
        CheckI("float constants 5 3", 35, InlF(CF(0xbf800000U), CF(0x80000000U)));
        CheckI("double constants 5 5", 26, InlD(CD(0xbff0000000000000UL), CD(0xbff0000000000000UL)));
        CheckI("float constants 5 6", 35, InlF(CF(0xbf800000U), CF(0x3f800001U)));
        CheckI("double constants 5 8", 35, InlD(CD(0xbff0000000000000UL), CD(0x8000000000000001UL)));
        CheckI("float constants 5 9", 35, InlF(CF(0xbf800000U), CF(0x7f7fffffU)));
        CheckI("double constants 5 11", 44, InlD(CD(0xbff0000000000000UL), CD(0xfff0000000000000UL)));
        CheckI("double constants 6 0", 32, InlD(CD(0x3ff0000000000001UL), CD(0x7ff8000000000000UL)));
        CheckI("float constants 6 1", 32, InlF(CF(0x3f800001U), CF(0xffc00001U)));
        CheckI("double constants 6 3", 44, InlD(CD(0x3ff0000000000001UL), CD(0x8000000000000000UL)));
        CheckI("float constants 6 4", 44, InlF(CF(0x3f800001U), CF(0x3f800000U)));
        CheckI("double constants 6 6", 26, InlD(CD(0x3ff0000000000001UL), CD(0x3ff0000000000001UL)));
        CheckI("float constants 6 7", 44, InlF(CF(0x3f800001U), CF(0x00000001U)));
        CheckI("double constants 6 9", 35, InlD(CD(0x3ff0000000000001UL), CD(0x7fefffffffffffffUL)));
        CheckI("float constants 6 10", 35, InlF(CF(0x3f800001U), CF(0x7f800000U)));
        CheckI("double constants 7 1", 32, InlD(CD(0x0000000000000001UL), CD(0xfff8000000000001UL)));
        CheckI("float constants 7 2", 44, InlF(CF(0x00000001U), CF(0x00000000U)));
        CheckI("double constants 7 4", 35, InlD(CD(0x0000000000000001UL), CD(0x3ff0000000000000UL)));
        CheckI("float constants 7 5", 44, InlF(CF(0x00000001U), CF(0xbf800000U)));
        CheckI("double constants 7 7", 26, InlD(CD(0x0000000000000001UL), CD(0x0000000000000001UL)));
        CheckI("float constants 7 8", 44, InlF(CF(0x00000001U), CF(0x80000001U)));
        CheckI("double constants 7 10", 35, InlD(CD(0x0000000000000001UL), CD(0x7ff0000000000000UL)));
        CheckI("float constants 7 11", 44, InlF(CF(0x00000001U), CF(0xff800000U)));
        CheckI("float constants 8 0", 32, InlF(CF(0x80000001U), CF(0x7fc00000U)));
        CheckI("double constants 8 2", 35, InlD(CD(0x8000000000000001UL), CD(0x0000000000000000UL)));
        CheckI("float constants 8 3", 35, InlF(CF(0x80000001U), CF(0x80000000U)));
        CheckI("double constants 8 5", 44, InlD(CD(0x8000000000000001UL), CD(0xbff0000000000000UL)));
        CheckI("float constants 8 6", 35, InlF(CF(0x80000001U), CF(0x3f800001U)));
        CheckI("double constants 8 8", 26, InlD(CD(0x8000000000000001UL), CD(0x8000000000000001UL)));
        CheckI("float constants 8 9", 35, InlF(CF(0x80000001U), CF(0x7f7fffffU)));
        CheckI("double constants 8 11", 44, InlD(CD(0x8000000000000001UL), CD(0xfff0000000000000UL)));
        CheckI("double constants 9 0", 32, InlD(CD(0x7fefffffffffffffUL), CD(0x7ff8000000000000UL)));
        CheckI("float constants 9 1", 32, InlF(CF(0x7f7fffffU), CF(0xffc00001U)));
        CheckI("double constants 9 3", 44, InlD(CD(0x7fefffffffffffffUL), CD(0x8000000000000000UL)));
        CheckI("float constants 9 4", 44, InlF(CF(0x7f7fffffU), CF(0x3f800000U)));
        CheckI("double constants 9 6", 44, InlD(CD(0x7fefffffffffffffUL), CD(0x3ff0000000000001UL)));
        CheckI("float constants 9 7", 44, InlF(CF(0x7f7fffffU), CF(0x00000001U)));
        CheckI("double constants 9 9", 26, InlD(CD(0x7fefffffffffffffUL), CD(0x7fefffffffffffffUL)));
        CheckI("float constants 9 10", 35, InlF(CF(0x7f7fffffU), CF(0x7f800000U)));
        CheckI("double constants 10 1", 32, InlD(CD(0x7ff0000000000000UL), CD(0xfff8000000000001UL)));
        CheckI("float constants 10 2", 44, InlF(CF(0x7f800000U), CF(0x00000000U)));
        CheckI("double constants 10 4", 44, InlD(CD(0x7ff0000000000000UL), CD(0x3ff0000000000000UL)));
        CheckI("float constants 10 5", 44, InlF(CF(0x7f800000U), CF(0xbf800000U)));
        CheckI("double constants 10 7", 44, InlD(CD(0x7ff0000000000000UL), CD(0x0000000000000001UL)));
        CheckI("float constants 10 8", 44, InlF(CF(0x7f800000U), CF(0x80000001U)));
        CheckI("double constants 10 10", 26, InlD(CD(0x7ff0000000000000UL), CD(0x7ff0000000000000UL)));
        CheckI("float constants 10 11", 44, InlF(CF(0x7f800000U), CF(0xff800000U)));
        CheckI("float constants 11 0", 32, InlF(CF(0xff800000U), CF(0x7fc00000U)));
        CheckI("double constants 11 2", 35, InlD(CD(0xfff0000000000000UL), CD(0x0000000000000000UL)));
        CheckI("float constants 11 3", 35, InlF(CF(0xff800000U), CF(0x80000000U)));
        CheckI("double constants 11 5", 35, InlD(CD(0xfff0000000000000UL), CD(0xbff0000000000000UL)));
        CheckI("float constants 11 6", 35, InlF(CF(0xff800000U), CF(0x3f800001U)));
        CheckI("double constants 11 8", 35, InlD(CD(0xfff0000000000000UL), CD(0x8000000000000001UL)));
        CheckI("float constants 11 9", 35, InlF(CF(0xff800000U), CF(0x7f7fffffU)));
        CheckI("double constants 11 11", 26, InlD(CD(0xfff0000000000000UL), CD(0xfff0000000000000UL)));
    }

    static int Main()
    {
        RunD();
        RunF();
        RunMixed();
        Folded();

        Console.WriteLine("checks " + checks);
        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails == 0 ? 0 : 1;
    }
}
