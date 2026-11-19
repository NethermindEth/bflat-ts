// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Exception handling through the byte-packed unwind tables of the runtime.
//
// Each method has a block of unwind data: one byte of flags followed by
// 32-bit offsets (to the block of the method a funclet belongs to, to the
// exception clauses, to associated data), with no padding, so an offset
// starts at any address. The runtime used to read these offsets through a
// pointer to int, a misaligned 4-byte load most of the time; it now copies
// the four bytes. What can go wrong: an offset put together in the wrong
// byte order or without its sign (offsets are negative as often as not), a
// pointer advanced by the wrong amount, so that the clauses of a method are
// not found, a funclet attributed to the wrong method, and a handler that is
// skipped or run twice.
//
// The methods here are many and of different sizes, so that their blocks
// start at every address modulo four. Exceptions are thrown and caught in
// the same method and several frames up, through finally blocks, filters and
// rethrows, out of catch and finally blocks (the throwing frame is a
// funclet), from handlers nested in handlers, in generic methods over
// reference and value types, in struct methods called through an interface
// (the frame of an unboxing stub lies in between), in lambdas, in iterators
// and in a method with a large frame, and many times in a loop.
//
// Every handler adds a digit to a log; the expected log is written next to
// each case and follows from the order the language defines. A run under an
// emulator that accepts misaligned accesses checks that order only; that the
// runtime makes no misaligned load needs a target that rejects them or a
// trace of its accesses. Each case prints one line; the run ends with ALL
// PASS or a FAIL count.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }
class E2 : E { public E2(int c) : base(c) { } }

interface IThrow
{
    long Caught(int k);
    long Escapes(int k);
}

struct Thrower : IThrow
{
    public long Seed;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Caught(int k)
    {
        try { Program.Throw(k); return -1; }
        catch (E e) when (e.Code > 0) { return e.Code * 2 + Seed; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Escapes(int k)
    {
        try { Program.Throw(k); return -1; }
        finally { Program.Log(8); }
    }
}

class Shared<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Run(T value, int k)
    {
        try
        {
            if (k > 0)
                throw new E(k);
            return (value == null) ? 1 : 2;
        }
        catch (E e)
        {
            Program.Log(e.Code);
            return (value == null) ? 3 : 4;
        }
        finally
        {
            Program.Log(9);
        }
    }
}

static class Program
{
    static long s_log;
    static int s_fails;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Log(int digit)
    {
        s_log = s_log * 10 + digit;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw(int k) => throw new E(k);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowKind(int kind)
    {
        switch (kind)
        {
            case 0: throw new ArgumentException("a");
            case 1: throw new InvalidOperationException("i");
            case 2: throw new E2(2);
            case 3: throw new E(3);
            case 4: throw new IndexOutOfRangeException();
            default: return;
        }
    }

    static void Check(string name, long expected, long got)
    {
        if (expected == got)
        {
            Console.WriteLine("ok   " + name);
        }
        else
        {
            s_fails++;
            Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long CatchSimple(int k)
    {
        try { Throw(k); return -1; }
        catch (E e) { return e.Code + 1; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long CatchTyped(int kind)
    {
        try { ThrowKind(kind); return 9; }
        catch (ArgumentException) { return 1; }
        catch (InvalidOperationException) { return 2; }
        catch (E2) { return 3; }
        catch (E) { return 4; }
        catch (Exception) { return 5; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long FinallyOrder()
    {
        s_log = 0;
        try
        {
            try { Log(1); Throw(1); }
            finally { Log(2); }
        }
        catch (E) { Log(3); }
        finally { Log(4); }
        return s_log;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Filter(int k)
    {
        s_log = 0;
        try { Throw(k); }
        catch (E e) when (Log(5) && e.Code == 7) { Log(6); }
        catch (E) { Log(7); }
        return s_log;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long NestedInCatch()
    {
        s_log = 0;
        try { Throw(1); }
        catch (E)
        {
            try { Log(1); Throw(2); }
            catch (E inner) { Log(inner.Code); }
            finally { Log(3); }
            Log(4);
        }
        return s_log;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Rethrow()
    {
        s_log = 0;
        try
        {
            try { Throw(1); }
            catch (E) { Log(1); throw; }
        }
        catch (E e) { Log(2 + e.Code); }
        return s_log;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ThrowFromCatch()
    {
        s_log = 0;
        try
        {
            try { Throw(1); }
            catch (E e) { Log(e.Code); throw new E2(4); }
            finally { Log(2); }
        }
        catch (E2 e) { Log(e.Code); }
        catch (E) { Log(9); }
        return s_log;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ThrowFromFinally()
    {
        s_log = 0;
        try
        {
            try { Throw(1); }
            finally { Log(1); Throw(5); }
        }
        catch (E e) { Log(e.Code); }
        return s_log;
    }

    // A handler in a handler in a handler: the innermost throw happens three funclets deep.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ThreeDeep()
    {
        s_log = 0;
        try
        {
            try { Throw(1); }
            finally
            {
                try { Log(1); Throw(2); }
                catch (E)
                {
                    try { Log(2); Throw(3); }
                    catch (E e) { Log(e.Code); }
                    finally { Log(4); }
                }
                Log(5);
            }
        }
        catch (E e) { Log(5 + e.Code); }
        return s_log;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Deep(int n, int k)
    {
        if (n == 0)
            Throw(k);
        try { Deep(n - 1, k); }
        finally { s_log++; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long DeepUnwind(int n)
    {
        s_log = 0;
        try { Deep(n, n); return -1; }
        catch (E e) { return s_log * 1000 + e.Code; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ThroughStruct(int k)
    {
        s_log = 0;
        IThrow t = new Thrower { Seed = 100 };
        long caught = t.Caught(k);
        try { t.Escapes(k); }
        catch (E e) { Log(e.Code); }
        return caught * 1000 + s_log;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Generics(int k)
    {
        s_log = 0;
        long r = new Shared<string>().Run("s", k);
        r = r * 10 + new Shared<object>().Run(null, k);
        r = r * 10 + new Shared<int>().Run(5, k);
        r = r * 10 + new Shared<long[]>().Run(null, 0);
        return r * 100000000 + s_log;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Lambdas(int k)
    {
        s_log = 0;
        int captured = k * 3;
        Func<int, long> f = x =>
        {
            try { Throw(x + captured); return -1; }
            catch (E e) { return e.Code; }
        };
        Action<int> g = x => { try { Throw(x); } finally { Log(1); } };
        long r = f(1);
        try { g(captured); }
        catch (E e) { Log(e.Code); }
        return r * 1000 + s_log;
    }

    static IEnumerable<int> Items(int count, int failAt)
    {
        try
        {
            for (int i = 0; i < count; i++)
            {
                if (i == failAt)
                    Throw(i);
                yield return i;
            }
        }
        finally
        {
            Log(7);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Iterators()
    {
        s_log = 0;
        long sum = 0;
        foreach (int i in Items(5, -1))
        {
            sum += i;
            if (i == 2)
                break;
        }
        try
        {
            foreach (int i in Items(5, 3))
                sum += i * 10;
        }
        catch (E e) { Log(e.Code); }
        return sum * 1000 + s_log;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe long LargeFrame(int k)
    {
        byte* buffer = stackalloc byte[6000];
        buffer[0] = 1;
        buffer[5999] = 2;
        long a = k, b = k * 3, c = k * 5;
        try { Throw(k); }
        catch (E e) { a += e.Code; }
        finally { b += buffer[0] + buffer[5999]; }
        return a * 1000000 + b * 1000 + c;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long InLoop(int count)
    {
        long sum = 0;
        for (int i = 0; i < count; i++)
        {
            try
            {
                if ((i & 1) == 0)
                    Throw(i);
                ThrowKind(i % 7);
                sum += 1;
            }
            catch (E e) { sum += e.Code; }
            catch (ArgumentException) { sum += 1000; }
            catch (Exception) { sum += 100000; }
        }
        return sum;
    }

    // Small methods of different sizes: their unwind blocks start at different addresses.
    [MethodImpl(MethodImplOptions.NoInlining)] static long V0(int k) { try { Throw(k); return 0; } catch (E e) { return e.Code; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static long V1(int k) { try { Throw(k); return 0; } catch (E e) { return e.Code + k; } finally { Log(1); } }
    [MethodImpl(MethodImplOptions.NoInlining)] static long V2(int k) { try { Throw(k); return 0; } catch (E e) when (e.Code == k) { return e.Code * 2; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static long V3(int k) { long a = k; try { Throw(k); } catch (E2) { a = -1; } catch (E e) { a += e.Code * 3; } return a; }
    [MethodImpl(MethodImplOptions.NoInlining)] static long V4(int k) { try { try { Throw(k); } finally { Log(2); } } catch (E e) { return e.Code * 5; } return 0; }
    [MethodImpl(MethodImplOptions.NoInlining)] static long V5(int k) { try { return V0(k) + V1(k); } catch (E) { return -1; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static long V6(int k) { try { ThrowKind(k % 5); return 0; } catch (ArgumentException) { return 1; } catch (Exception) { return 2; } }
    [MethodImpl(MethodImplOptions.NoInlining)] static long V7(int k) { long a = 0; for (int i = 0; i < 3; i++) { try { Throw(k + i); } catch (E e) { a += e.Code; } } return a; }
    [MethodImpl(MethodImplOptions.NoInlining)] static long V8(int k) { try { Throw(k); } catch (E e) { try { Throw(e.Code + 1); } catch (E f) { return f.Code; } } return 0; }
    [MethodImpl(MethodImplOptions.NoInlining)] static long V9(int k) { try { Throw(k); return 0; } catch (E e) when (Log(3)) { return e.Code + 9; } finally { Log(4); } }

    static int Main()
    {
        Check("CatchSimple", 42, CatchSimple(41));
        Check("CatchTyped", 12345, CatchTyped(0) * 10000 + CatchTyped(1) * 1000 + CatchTyped(2) * 100 + CatchTyped(3) * 10 + CatchTyped(4));
        Check("CatchTyped no throw", 9, CatchTyped(5));
        Check("FinallyOrder", 1234, FinallyOrder());
        Check("Filter accepts", 56, Filter(7));
        Check("Filter rejects", 57, Filter(8));
        Check("NestedInCatch", 1234, NestedInCatch());
        Check("Rethrow", 13, Rethrow());
        Check("ThrowFromCatch", 124, ThrowFromCatch());
        Check("ThrowFromFinally", 15, ThrowFromFinally());
        Check("ThreeDeep", 123456, ThreeDeep());
        Check("DeepUnwind 1", 1001, DeepUnwind(1));
        Check("DeepUnwind 40", 40040, DeepUnwind(40));
        Check("ThroughStruct", 110000 + 85, ThroughStruct(5));
        Check("Generics", 4341L * 100000000 + 3939399, Generics(3));
        Check("Lambdas", 7000 + 16, Lambdas(2));
        Check("Iterators", 33000 + 773, Iterators());
        Check("LargeFrame", 14L * 1000000 + 24 * 1000 + 35, LargeFrame(7));

        long loop = 0;
        for (int i = 0; i < 700; i++)
        {
            if ((i & 1) == 0) loop += i;
            else if (i % 7 == 0) loop += 1000;
            else if (i % 7 == 1 || i % 7 == 4) loop += 100000;
            else if (i % 7 == 2) loop += 2;
            else if (i % 7 == 3) loop += 3;
            else loop += 1;
        }
        Check("InLoop", loop, InLoop(700));

        s_log = 0;
        long v = 0;
        for (int k = 1; k <= 20; k++)
            v = v * 3 + V0(k) + V1(k) + V2(k) + V3(k) + V4(k) + V5(k) + V6(k) + V7(k) + V8(k) + V9(k);
        long want = 0;
        for (int k = 1; k <= 20; k++)
        {
            long v6 = (k % 5 == 0) ? 1 : (k % 5 < 5 && k % 5 != 0) ? 2 : 0;
            want = want * 3 + k + 2 * k + 2 * k + 4 * k + 5 * k + (k + 2 * k) + v6 + (3 * k + 3) + (k + 1) + (k + 9);
        }
        Check("small methods", want, v);
        // V1, V4, V5 (through V1) and V9 log 1, 2, 1 and 3 4 for each k; the log overflows and wraps the same way every run.
        long log = 0;
        for (int k = 1; k <= 20; k++)
        {
            log = log * 10 + 1; log = log * 10 + 2; log = log * 10 + 1; log = log * 10 + 3; log = log * 10 + 4;
        }
        Check("small methods log", log, s_log);

        Console.WriteLine(s_fails == 0 ? "ALL PASS" : "FAILED " + s_fails);
        return s_fails == 0 ? 0 : 1;
    }
}
