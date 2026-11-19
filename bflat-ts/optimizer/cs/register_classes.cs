// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Methods whose values compete for caller-saved and callee-saved registers.
//
// On RISC-V the register allocator picks the save class of a register
// before it looks at how long the register is free
// (JitRiscV64LsraCallerCalleeFirst, on by default; 0 gives the stock
// order). A value that does not span a call goes to a caller-saved
// register whenever one is free, so methods save fewer registers, and
// values end up split, spilled around calls and moved at block boundaries
// in places where the stock order kept them in one callee-saved register.
// The test passes with either order.
//
// The methods are built to make that choice matter: a leaf method with more
// live values than there are caller-saved registers; a loop where values
// span a call only on a rare path; values that are live around a call but
// dead on the path that makes it, or defined again by it, as in lazy
// initialization; a loop with a call in every iteration
// and temporaries between the calls; nested loops with a switch where some
// arms call and others do not; twelve parameters, four of them on the
// stack; an instance method working on fields and an array; divisions,
// which are calls on a 32-bit target; a recursive method that an exception
// unwinds through, caught by a frame that keeps its own values across the
// try.
//
// Every method has a twin with the same body that is compiled without
// optimization, and the results must agree. Each check prints one line; the
// run ends with ALL PASS or a FAIL count, and the guest returns the number
// of failures.
using System;
using System.Runtime.CompilerServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

class Acc
{
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    public long A, B;
    public long[] Arr = new long[8];

    public void Reset(int seed)
    {
        A = Program.Mix(seed, 20);
        B = Program.Mix(seed, 21);
        for (int i = 0; i < Arr.Length; i++) Arr[i] = Program.Mix(seed, 30 + i);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Run(long x, long y, int n)
    {
        long s = A;
        for (int i = 0; i < n; i++)
        {
            long t = Arr[i & 7] * x + y, u = t ^ B, v = u + (s >> 3);
            s += u;
            if ((t & 7) == 0) Arr[(i + 1) & 7] = Program.MixL(v, i) + u;
            A += v ^ x;
            y += t >> 9;
        }
        return s + A * 3 + B * 5 + y;
    }

    [MethodImpl(NoOpt)]
    public long RunRef(long x, long y, int n)
    {
        long s = A;
        for (int i = 0; i < n; i++)
        {
            long t = Arr[i & 7] * x + y, u = t ^ B, v = u + (s >> 3);
            s += u;
            if ((t & 7) == 0) Arr[(i + 1) & 7] = Program.MixL(v, i) + u;
            A += v ^ x;
            y += t >> 9;
        }
        return s + A * 3 + B * 5 + y;
    }
}

class Cell { public long V; public Cell Next; public long[] Data; }

class Program
{
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;
    const long Mult = unchecked((long)0x9E3779B97F4A7C15UL);

    static int fails;

    static void Check(string name, bool ok)
    {
        Console.WriteLine((ok ? "ok   " : "FAIL ") + name);
        if (!ok) fails++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Mix(int seed, int i) => (seed + 1) * Mult + i * 0x100000001B3L;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long MixL(long a, long b) => (a + 1) * Mult + b * 0x100000001B3L;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Thrower(int code)
    {
        if (code >= 0) throw new E(code);
        return 0;
    }

    // ---- a leaf with twenty-two live values ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Leaf(long a, long b, long c, long d, long e, long f, long g, long h)
    {
        long t0 = a * b + c, t1 = b * c + d, t2 = c * d + e, t3 = d * e + f, t4 = e * f + g, t5 = f * g + h;
        long t6 = g * h + a, t7 = h * a + b, t8 = t0 ^ t4, t9 = t1 ^ t5, t10 = t2 ^ t6, t11 = t3 ^ t7;
        long t12 = t8 * t9, t13 = t10 * t11;
        return (a ^ t0) + (b ^ t1) * 3 + (c ^ t2) * 5 + (d ^ t3) * 7 + (e ^ t4) * 9 + (f ^ t5) * 11
               + (g ^ t6) * 13 + (h ^ t7) * 15 + t8 * 17 + t9 * 19 + t10 * 21 + t11 * 23 + t12 * 25 + t13 * 27;
    }

    [MethodImpl(NoOpt)]
    static long LeafRef(long a, long b, long c, long d, long e, long f, long g, long h)
    {
        long t0 = a * b + c, t1 = b * c + d, t2 = c * d + e, t3 = d * e + f, t4 = e * f + g, t5 = f * g + h;
        long t6 = g * h + a, t7 = h * a + b, t8 = t0 ^ t4, t9 = t1 ^ t5, t10 = t2 ^ t6, t11 = t3 ^ t7;
        long t12 = t8 * t9, t13 = t10 * t11;
        return (a ^ t0) + (b ^ t1) * 3 + (c ^ t2) * 5 + (d ^ t3) * 7 + (e ^ t4) * 9 + (f ^ t5) * 11
               + (g ^ t6) * 13 + (h ^ t7) * 15 + t8 * 17 + t9 * 19 + t10 * 21 + t11 * 23 + t12 * 25 + t13 * 27;
    }

    // ---- values that span a call only on a rare path ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long RareCall(long a, long b, long c, long d, int n)
    {
        long x = a, y = b, z = c, w = d, acc = 0;
        for (int i = 0; i < n; i++)
        {
            long p = x * y + i, q = z ^ (w + i), r = p + q;
            x += r; y ^= p; z += q * 3; w -= r >> 3;
            if ((r & 31) == 0) acc += MixL(x, i) + p - q;
            acc += x ^ y ^ z ^ w;
        }
        return acc + x + y * 3 + z * 5 + w * 7;
    }

    [MethodImpl(NoOpt)]
    static long RareCallRef(long a, long b, long c, long d, int n)
    {
        long x = a, y = b, z = c, w = d, acc = 0;
        for (int i = 0; i < n; i++)
        {
            long p = x * y + i, q = z ^ (w + i), r = p + q;
            x += r; y ^= p; z += q * 3; w -= r >> 3;
            if ((r & 31) == 0) acc += MixL(x, i) + p - q;
            acc += x ^ y ^ z ^ w;
        }
        return acc + x + y * 3 + z * 5 + w * 7;
    }

    // ---- values that are dead on the paths with calls ----

    // The temporaries are live around a call but not across it: the call
    // is on a path where they are no longer needed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Lazy(long a, long b, long c, long d)
    {
        long t0 = a * b, t1 = c * d, t2 = a ^ d;
        long r;
        if ((t0 & 3) == 0) r = MixL(a, b);
        else r = t0 + t1 * 3 + t2 * 5;
        long u0 = r * c, u1 = r ^ d;
        if ((u0 & 3) == 0) return MixL(r, c) + 1;
        return u0 + u1 * 7;
    }

    [MethodImpl(NoOpt)]
    static long LazyRef(long a, long b, long c, long d)
    {
        long t0 = a * b, t1 = c * d, t2 = a ^ d;
        long r;
        if ((t0 & 3) == 0) r = MixL(a, b);
        else r = t0 + t1 * 3 + t2 * 5;
        long u0 = r * c, u1 = r ^ d;
        if ((u0 & 3) == 0) return MixL(r, c) + 1;
        return u0 + u1 * 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long EarlyOut(long a, long b, int n)
    {
        long x = a, acc = b;
        for (int i = 0; i < n; i++)
        {
            long t = x * 31 + i, u = t ^ (acc >> 7);
            if ((u & 7) == 0)
            {
                acc += MixL(x, i);
                continue;
            }
            long v = t * u + 1, w = v ^ t;
            acc += w;
            x += v >> 5;
        }
        return acc ^ x;
    }

    [MethodImpl(NoOpt)]
    static long EarlyOutRef(long a, long b, int n)
    {
        long x = a, acc = b;
        for (int i = 0; i < n; i++)
        {
            long t = x * 31 + i, u = t ^ (acc >> 7);
            if ((u & 7) == 0)
            {
                acc += MixL(x, i);
                continue;
            }
            long v = t * u + 1, w = v ^ t;
            acc += w;
            x += v >> 5;
        }
        return acc ^ x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long[] MakeData(Cell c)
    {
        c.Data = new long[] { c.V * 3, c.V * 5 };
        return c.Data;
    }

    // Null tests with a call on one side, as in lazy initialization.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Chain(Cell c, long k)
    {
        Cell n = c.Next;
        if (n == null) return MixL(c.V, k);
        long[] d = n.Data;
        if (d == null) d = MakeData(n);
        long v = d[0] * k;
        Cell m = n.Next;
        if (m == null) return v + d[1];
        long w = m.V ^ v;
        if ((w & 1) == 0) return MixL(w, v);
        return w * 3 + d[1] + n.V;
    }

    [MethodImpl(NoOpt)]
    static long ChainRef(Cell c, long k)
    {
        Cell n = c.Next;
        if (n == null) return MixL(c.V, k);
        long[] d = n.Data;
        if (d == null) d = MakeData(n);
        long v = d[0] * k;
        Cell m = n.Next;
        if (m == null) return v + d[1];
        long w = m.V ^ v;
        if ((w & 1) == 0) return MixL(w, v);
        return w * 3 + d[1] + n.V;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Cell NextOf(Cell c) => c.Next ?? c;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long AfterCall(Cell c)
    {
        Cell n = NextOf(c);
        long[] d = n.Data;
        if (d == null)
        {
            MakeData(n);
            return n.V;
        }
        return d[0] + d[1] * 3;
    }

    [MethodImpl(NoOpt)]
    static long AfterCallRef(Cell c)
    {
        Cell n = NextOf(c);
        long[] d = n.Data;
        if (d == null)
        {
            MakeData(n);
            return n.V;
        }
        return d[0] + d[1] * 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Walk(Cell head, long k)
    {
        long acc = k;
        for (Cell c = head; c != null; c = c.Next)
        {
            long[] d = c.Data;
            long v = c.V * k;
            if (d == null)
            {
                acc += MixL(v, 1);
                continue;
            }
            long t = d[0] ^ v, u = d[1] + t;
            acc += t * u;
        }
        return acc;
    }

    [MethodImpl(NoOpt)]
    static long WalkRef(Cell head, long k)
    {
        long acc = k;
        for (Cell c = head; c != null; c = c.Next)
        {
            long[] d = c.Data;
            long v = c.V * k;
            if (d == null)
            {
                acc += MixL(v, 1);
                continue;
            }
            long t = d[0] ^ v, u = d[1] + t;
            acc += t * u;
        }
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Exits(long a, long b, long[] arr)
    {
        long t0 = arr[0] * a, t1 = arr[1] * b, t2 = t0 ^ t1, t3 = t0 + t1;
        if ((t2 & 3) == 0) return MixL(a, b);
        long u = t2 * t3 + t0;
        if ((u & 1) == 0) return MixL(u, a) ^ b;
        return u + t1 + t3;
    }

    [MethodImpl(NoOpt)]
    static long ExitsRef(long a, long b, long[] arr)
    {
        long t0 = arr[0] * a, t1 = arr[1] * b, t2 = t0 ^ t1, t3 = t0 + t1;
        if ((t2 & 3) == 0) return MixL(a, b);
        long u = t2 * t3 + t0;
        if ((u & 1) == 0) return MixL(u, a) ^ b;
        return u + t1 + t3;
    }

    // A value that a call on one path defines again: it is live on both
    // sides of the call and not across it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Refill(long a, long b, long c, int n)
    {
        long x = a, y = b, acc = 0;
        for (int i = 0; i < n; i++)
        {
            long t = x * 3 + i;
            if ((t & 7) == 0) t = MixL(x, i);
            long u = t ^ y;
            if ((u & 15) == 0) u = MixL(u, c) + 1;
            acc += t * u;
            x += u >> 4;
            y ^= t;
        }
        return acc ^ x ^ y;
    }

    [MethodImpl(NoOpt)]
    static long RefillRef(long a, long b, long c, int n)
    {
        long x = a, y = b, acc = 0;
        for (int i = 0; i < n; i++)
        {
            long t = x * 3 + i;
            if ((t & 7) == 0) t = MixL(x, i);
            long u = t ^ y;
            if ((u & 15) == 0) u = MixL(u, c) + 1;
            acc += t * u;
            x += u >> 4;
            y ^= t;
        }
        return acc ^ x ^ y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Coalesce(Cell c, long k)
    {
        long[] d = c.Data ?? MakeData(c);
        Cell n = c.Next ?? NextOf(c);
        long[] e = n.Data ?? MakeData(n);
        long v = d[0] * k;
        if ((v & 1) == 0) v = MixL(v, e[0]);
        return v + e[1] + n.V;
    }

    [MethodImpl(NoOpt)]
    static long CoalesceRef(Cell c, long k)
    {
        long[] d = c.Data ?? MakeData(c);
        Cell n = c.Next ?? NextOf(c);
        long[] e = n.Data ?? MakeData(n);
        long v = d[0] * k;
        if ((v & 1) == 0) v = MixL(v, e[0]);
        return v + e[1] + n.V;
    }

    static Cell MakeChain(int seed, int length, bool data)
    {
        Cell head = null;
        for (int i = length - 1; i >= 0; i--)
        {
            Cell c = new Cell { V = Mix(seed, i), Next = head };
            if (data) c.Data = new long[] { Mix(seed, 40 + i), Mix(seed, 50 + i) };
            head = c;
        }
        return head;
    }

    // ---- a call in every iteration, temporaries between the calls ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long CallEvery(long a, long b, int n)
    {
        long acc = a, k = b;
        for (int i = 0; i < n; i++)
        {
            long t0 = acc * 3 + i, t1 = k ^ t0, t2 = t0 + t1 * 5, t3 = t2 >> 7, t4 = t3 ^ acc;
            long m = MixL(t4, i);
            long u0 = m + t0, u1 = m ^ k, u2 = u0 * u1;
            acc += u2 + t1;
            k = k * 31 + MixL(i, u0) + u1;
        }
        return acc ^ k;
    }

    [MethodImpl(NoOpt)]
    static long CallEveryRef(long a, long b, int n)
    {
        long acc = a, k = b;
        for (int i = 0; i < n; i++)
        {
            long t0 = acc * 3 + i, t1 = k ^ t0, t2 = t0 + t1 * 5, t3 = t2 >> 7, t4 = t3 ^ acc;
            long m = MixL(t4, i);
            long u0 = m + t0, u1 = m ^ k, u2 = u0 * u1;
            acc += u2 + t1;
            k = k * 31 + MixL(i, u0) + u1;
        }
        return acc ^ k;
    }

    // ---- a switch in nested loops, some arms with calls ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long SwitchLoop(long a, long b, long c, int n)
    {
        long x = a, y = b, z = c, acc = 0;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                switch ((int)((x ^ i ^ j) & 7))
                {
                    case 0: x += y * 3; break;
                    case 1: y ^= z + i; break;
                    case 2: z += Mix(i, j); break;
                    case 3: acc += x ^ y; x = x * 5 + 1; break;
                    case 4: y += MixL(j, z) ^ x; break;
                    case 5: z = z * 7 + y; acc ^= z; break;
                    case 6: acc += Thrower(-1) + x; x += 3; break;
                    default: x ^= z >> 5; y += acc; break;
                }
            }
            acc += x + y + z;
        }
        return acc ^ x ^ y * 3 ^ z * 5;
    }

    [MethodImpl(NoOpt)]
    static long SwitchLoopRef(long a, long b, long c, int n)
    {
        long x = a, y = b, z = c, acc = 0;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                switch ((int)((x ^ i ^ j) & 7))
                {
                    case 0: x += y * 3; break;
                    case 1: y ^= z + i; break;
                    case 2: z += Mix(i, j); break;
                    case 3: acc += x ^ y; x = x * 5 + 1; break;
                    case 4: y += MixL(j, z) ^ x; break;
                    case 5: z = z * 7 + y; acc ^= z; break;
                    case 6: acc += Thrower(-1) + x; x += 3; break;
                    default: x ^= z >> 5; y += acc; break;
                }
            }
            acc += x + y + z;
        }
        return acc ^ x ^ y * 3 ^ z * 5;
    }

    // ---- twelve parameters, four of them on the stack ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Many(long a0, long a1, long a2, long a3, long a4, long a5,
                     long a6, long a7, long a8, long a9, long a10, long a11, int n)
    {
        long acc = 0;
        for (int i = 0; i < n; i++)
        {
            long t = a0 * a11 + a1 * a10 + a2 * a9 + a3 * a8 + a4 * a7 + a5 * a6 + i;
            acc += t ^ (acc >> 5);
            if ((t & 15) == 0) acc += MixL(t, i);
            a0 += a6; a1 ^= a7; a2 += a8; a3 ^= a9; a4 += a10; a5 ^= a11;
        }
        return acc + a0 + a1 * 3 + a2 * 5 + a3 * 7 + a4 * 9 + a5 * 11;
    }

    [MethodImpl(NoOpt)]
    static long ManyRef(long a0, long a1, long a2, long a3, long a4, long a5,
                        long a6, long a7, long a8, long a9, long a10, long a11, int n)
    {
        long acc = 0;
        for (int i = 0; i < n; i++)
        {
            long t = a0 * a11 + a1 * a10 + a2 * a9 + a3 * a8 + a4 * a7 + a5 * a6 + i;
            acc += t ^ (acc >> 5);
            if ((t & 15) == 0) acc += MixL(t, i);
            a0 += a6; a1 ^= a7; a2 += a8; a3 ^= a9; a4 += a10; a5 ^= a11;
        }
        return acc + a0 + a1 * 3 + a2 * 5 + a3 * 7 + a4 * 9 + a5 * 11;
    }

    // ---- divisions ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Div(long a, long b, int c, int d)
    {
        long db = (b & 0xFFFF) + 1;
        int dd = (d & 0xFF) + 1;
        long q = a / db, r = a % db;
        int e = c / dd, f = c % dd;
        ulong uq = (ulong)a / (ulong)db, ur = (ulong)b % (ulong)(dd + 2);
        long g = q * 3 + r, h = e * 5L + f;
        return g ^ h ^ (long)uq * 7 ^ (long)ur * 11 ^ (a + b) ^ (c * 13L + d) ^ q ^ r * 17 ^ e ^ f * 19;
    }

    [MethodImpl(NoOpt)]
    static long DivRef(long a, long b, int c, int d)
    {
        long db = (b & 0xFFFF) + 1;
        int dd = (d & 0xFF) + 1;
        long q = a / db, r = a % db;
        int e = c / dd, f = c % dd;
        ulong uq = (ulong)a / (ulong)db, ur = (ulong)b % (ulong)(dd + 2);
        long g = q * 3 + r, h = e * 5L + f;
        return g ^ h ^ (long)uq * 7 ^ (long)ur * 11 ^ (a + b) ^ (c * 13L + d) ^ q ^ r * 17 ^ e ^ f * 19;
    }

    // ---- an exception through frames with both kinds of values ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Deep(int depth, int code, long a, long b, long c)
    {
        long t0 = a * 3 + b, t1 = b * 5 + c, t2 = c * 7 + a, t3 = t0 ^ t1, t4 = t1 ^ t2, t5 = t2 ^ t0;
        long u = t3 * t4 + t5;
        long r = depth == 0 ? Thrower(code) : Deep(depth - 1, code, u, t0, t1);
        long w0 = r + t2, w1 = w0 * t3, w2 = w1 ^ t4;
        return w2 + t5 * 3 + u;
    }

    [MethodImpl(NoOpt)]
    static long DeepRef(int depth, int code, long a, long b, long c)
    {
        long t0 = a * 3 + b, t1 = b * 5 + c, t2 = c * 7 + a, t3 = t0 ^ t1, t4 = t1 ^ t2, t5 = t2 ^ t0;
        long u = t3 * t4 + t5;
        long r = depth == 0 ? Thrower(code) : DeepRef(depth - 1, code, u, t0, t1);
        long w0 = r + t2, w1 = w0 * t3, w2 = w1 ^ t4;
        return w2 + t5 * 3 + u;
    }

    // Eight values kept across the try, six temporaries that end before it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CatchKeep(int depth, int code, int seed)
    {
        long k0 = Mix(seed, 0), k1 = Mix(seed, 1), k2 = Mix(seed, 2), k3 = Mix(seed, 3);
        long k4 = Mix(seed, 4), k5 = Mix(seed, 5), k6 = Mix(seed, 6), k7 = Mix(seed, 7);
        long t0 = k0 * k1, t1 = k2 * k3, t2 = k4 * k5, t3 = k6 * k7, t4 = t0 ^ t1, t5 = t2 ^ t3;
        long a = t4 + t5, b = t4 * t5;
        int got = -1;
        long r = 0;
        try
        {
            r = Deep(depth, code, a, b, k0);
        }
        catch (E e)
        {
            got = e.Code;
        }
        long sum = k0 ^ k1 * 3 ^ k2 * 5 ^ k3 * 7 ^ k4 * 9 ^ k5 * 11 ^ k6 * 13 ^ k7 * 15;
        long want = Mix(seed, 0) ^ Mix(seed, 1) * 3 ^ Mix(seed, 2) * 5 ^ Mix(seed, 3) * 7
                    ^ Mix(seed, 4) * 9 ^ Mix(seed, 5) * 11 ^ Mix(seed, 6) * 13 ^ Mix(seed, 7) * 15;
        if (sum != want) return false;
        if (code >= 0) return got == code;
        return got == -1 && r == DeepRef(depth, code, a, b, k0);
    }

    static int Main()
    {
        bool leaf = true, rare = true, every = true, sw = true, many = true, div = true, inst = true;
        bool lazy = true, early = true, chain = true, exits = true, refill = true;
        long[] arr2 = new long[2];
        Acc acc = new Acc(), accRef = new Acc();
        for (int seed = 0; seed < 40; seed++)
        {
            long a = Mix(seed, 0), b = Mix(seed, 1), c = Mix(seed, 2), d = Mix(seed, 3);
            long e = Mix(seed, 4), f = Mix(seed, 5), g = Mix(seed, 6), h = Mix(seed, 7);
            int n = 20 + seed * 3;

            leaf &= Leaf(a, b, c, d, e, f, g, h) == LeafRef(a, b, c, d, e, f, g, h);
            rare &= RareCall(a, b, c, d, n * 4) == RareCallRef(a, b, c, d, n * 4);
            lazy &= Lazy(a, b, c, d) == LazyRef(a, b, c, d) && Lazy(seed, a, 4, b) == LazyRef(seed, a, 4, b);
            lazy &= Lazy(seed, 3, c, d) == LazyRef(seed, 3, c, d);
            early &= EarlyOut(a, b, n * 4) == EarlyOutRef(a, b, n * 4);
            refill &= Refill(a, b, c, n * 4) == RefillRef(a, b, c, n * 4);
            arr2[0] = e; arr2[1] = f;
            exits &= Exits(a, b, arr2) == ExitsRef(a, b, arr2) && Exits(seed, c, arr2) == ExitsRef(seed, c, arr2);
            for (int length = 1; length <= 3; length++)
            {
                refill &= Coalesce(MakeChain(seed, length, false), a) == CoalesceRef(MakeChain(seed, length, false), a);
                refill &= Coalesce(MakeChain(seed, length, true), b) == CoalesceRef(MakeChain(seed, length, true), b);
                chain &= AfterCall(MakeChain(seed, length, false)) == AfterCallRef(MakeChain(seed, length, false));
                chain &= AfterCall(MakeChain(seed, length, true)) == AfterCallRef(MakeChain(seed, length, true));
                Cell mixed = MakeChain(seed, length + 2, true), mixedRef = MakeChain(seed, length + 2, true);
                mixed.Next.Data = null; mixedRef.Next.Data = null;
                chain &= Walk(mixed, c) == WalkRef(mixedRef, c);
                chain &= Chain(MakeChain(seed, length, false), a) == ChainRef(MakeChain(seed, length, false), a);
                chain &= Chain(MakeChain(seed, length, true), b) == ChainRef(MakeChain(seed, length, true), b);
            }
            every &= CallEvery(a, b, n) == CallEveryRef(a, b, n);
            sw &= SwitchLoop(a, b, c, n) == SwitchLoopRef(a, b, c, n);
            many &= Many(a, b, c, d, e, f, g, h, a ^ e, b ^ f, c ^ g, d ^ h, n)
                    == ManyRef(a, b, c, d, e, f, g, h, a ^ e, b ^ f, c ^ g, d ^ h, n);
            int ci = seed * 0x01234567 + 99, di = seed * 77 - 1000;
            div &= Div(a, b, ci, di) == DivRef(a, b, ci, di);
            div &= Div(-a, seed, -ci, ~di) == DivRef(-a, seed, -ci, ~di);

            acc.Reset(seed);
            accRef.Reset(seed);
            inst &= acc.Run(a, b, n * 2) == accRef.RunRef(a, b, n * 2);
            inst &= acc.A == accRef.A && acc.Arr[seed & 7] == accRef.Arr[seed & 7];
        }
        Check("leaf", leaf);
        Check("rare call", rare);
        Check("dead on the call path", lazy);
        Check("call on the early exit", early);
        Check("lazy initialization", chain);
        Check("calls on the exits", exits);
        Check("defined again by a call", refill);
        Check("call in every iteration", every);
        Check("switch in loops", sw);
        Check("stack parameters", many);
        Check("divisions", div);
        Check("instance method", inst);

        bool thrown = true, returned = true;
        for (int depth = 0; depth < 12; depth++)
        {
            returned &= CatchKeep(depth, -1, depth);
            thrown &= CatchKeep(depth, 30 + depth, depth * 5);
        }
        Check("deep return", returned);
        Check("deep throw", thrown);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
