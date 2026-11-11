// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Exception dispatch beyond catch + finally: filters, rethrow, handlers that
// throw, deep stacks, generic virtual and interface dispatch (unboxing stubs),
// delegates, iterators, static constructors and the runtime throw helpers.
// Each case prints one line; the run ends with ALL PASS or a FAIL count.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

class E1 : Exception { public int Code; public E1(int c) : base("e1") { Code = c; } }
class E2 : Exception { public E2(string m, Exception inner) : base(m, inner) { } }

interface IThrower { int Go(int x); }
struct SThrower<T> : IThrower
{
    public T Tag; public int Limit;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Go(int x) { if (x > Limit) throw new E1(x); return x + Limit; }
}

abstract class Base { public abstract int V<T>(T v, int n); }
class Derived : Base
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int V<T>(T v, int n) { if (n == 0) throw new E1(typeof(T) == typeof(string) ? 7 : 8); return V(v, n - 1) + 1; }
}

static class Bad
{
    public static int X = Init();
    static int Init() => throw new E1(99);
}

sealed class Res : IDisposable { public static int Disposed; public void Dispose() => Disposed++; }

class Program
{
    static int fails;
    static readonly List<string> log = new List<string>();

    static void Check(string name, bool ok)
    {
        Console.WriteLine((ok ? "ok   " : "FAIL ") + name);
        if (!ok) fails++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Throw(int c) => throw new E1(c);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Note(string s, bool r) { log.Add(s); return r; }

    static int Filters()
    {
        log.Clear();
        try
        {
            try { Throw(3); }
            catch (E1 e) when (Note("f1", e.Code == 4)) { return -1; }
            finally { log.Add("fin"); }
        }
        catch (E1 e) when (Note("f2", e.Code == 3)) { log.Add("c2"); return e.Code; }
        return -2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool FilterThrows(Exception e) => throw new InvalidOperationException("in filter");

    static int FilterThatThrows()
    {
        try
        {
            try { Throw(5); }
            catch (E1 e) when (FilterThrows(e)) { return -1; }
        }
        catch (E1 e) { return e.Code; }
        return -2;
    }

    static int Rethrow()
    {
        try
        {
            try { Throw(11); }
            catch (E1) { log.Add("r1"); throw; }
        }
        catch (E1 e) { return e.Code; }
        return -1;
    }

    static string Wrap()
    {
        try
        {
            try { Throw(12); }
            catch (E1 e) { throw new E2("wrapped", e); }
        }
        catch (E2 e) { return e.Message + "/" + ((E1)e.InnerException).Code; }
        return "none";
    }

    static int ThrowInCatchHandledInside()
    {
        int r = 0;
        try { Throw(1); }
        catch (E1)
        {
            try { Throw(2); }
            catch (E1 e2) { r = e2.Code * 10; }
            r += 1;
        }
        return r;
    }

    static int ThrowInFinally()
    {
        try
        {
            try { Throw(1); }
            finally { Throw(21); }
        }
        catch (E1 e) { return e.Code; }
        return -1;
    }

    static int finallies;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Deep(int n)
    {
        try
        {
            if (n == 0) Throw(31);
            return Deep(n - 1) + 1;
        }
        finally { finallies++; }
    }

    static int DeepStack()
    {
        finallies = 0;
        try { Deep(60); } catch (E1 e) { return e.Code * 1000 + finallies; }
        return -1;
    }

    static int ViaInterface<T>(T t) where T : IThrower
    {
        try { return t.Go(10); } catch (E1 e) { return -e.Code; }
    }

    static int BoxedInterface()
    {
        IThrower a = new SThrower<string> { Tag = "s", Limit = 3 };
        IThrower b = new SThrower<int> { Tag = 1, Limit = 20 };
        int r = 0;
        try { r += a.Go(4); } catch (E1 e) { r += e.Code * 100; }
        r += b.Go(5);
        r += ViaInterface(new SThrower<object> { Limit = 1 });
        return r;
    }

    static int Gvm()
    {
        Base b = new Derived();
        int r = 0;
        try { b.V("x", 5); } catch (E1 e) { r += e.Code; }
        try { b.V(42, 3); } catch (E1 e) { r += e.Code * 10; }
        return r;
    }

    static int Delegates()
    {
        Func<int, int> f = x => x > 2 ? throw new E1(x) : x;
        IThrower t = new SThrower<long> { Limit = 0 };
        Func<int, int> g = t.Go;
        int r = 0;
        try { r += f(1); r += f(3); } catch (E1 e) { r += e.Code * 10; }
        try { g(9); } catch (E1 e) { r += e.Code * 100; }
        return r;
    }

    static IEnumerable<int> Iter()
    {
        try
        {
            yield return 1;
            yield return 2;
            Throw(41);
        }
        finally { log.Add("iterfin"); }
    }

    static int Iterator()
    {
        log.Clear();
        int s = 0;
        try { foreach (int i in Iter()) s += i; } catch (E1 e) { s += e.Code; }
        return log.Contains("iterfin") ? s : -1;
    }

    static int StaticCtor()
    {
        try { return Bad.X; }
        catch (TypeInitializationException e) { return ((E1)e.InnerException).Code; }
    }

    static int Using()
    {
        Res.Disposed = 0;
        try { using (new Res()) using (new Res()) Throw(1); } catch (E1) { }
        return Res.Disposed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Div(int a, int b) => a / b;

    static string Helpers(int zero, object o, int[] arr)
    {
        string r = "";
        try { Div(1, zero); } catch (DivideByZeroException) { r += "div "; }
        try { checked { int x = int.MaxValue; x += arr.Length; r += x; } } catch (OverflowException) { r += "ovf "; }
        try { r += arr[arr.Length]; } catch (IndexOutOfRangeException) { r += "idx "; }
        try { r += ((string)o).Length; } catch (InvalidCastException) { r += "cast "; }
        try { r += int.Parse("x1"); } catch (FormatException) { r += "fmt "; }
        try { var d = new Dictionary<int, int>(); r += d[5]; } catch (KeyNotFoundException) { r += "key"; }
        return r;
    }

    static int ReturnInFinally()
    {
        int x = 0;
        for (int i = 0; i < 3; i++)
        {
            try { if (i == 1) Throw(i); x += 1; }
            catch (E1) { x += 10; continue; }
            finally { x += 100; }
        }
        return x;
    }

    static int Main()
    {
        int f = Filters();
        Check("filter " + f + " " + string.Join(",", log), f == 3 && string.Join(",", log) == "f1,f2,fin,c2");
        Check("filter that throws", FilterThatThrows() == 5);
        log.Clear();
        Check("rethrow", Rethrow() == 11 && log.Count == 1);
        Check("wrap " + Wrap(), Wrap() == "wrapped/12");
        Check("throw in catch", ThrowInCatchHandledInside() == 21);
        Check("throw in finally", ThrowInFinally() == 21);
        int d = DeepStack();
        Check("deep " + d, d == 31061);
        int bi = BoxedInterface();
        Check("boxed interface " + bi, bi == 400 + 25 - 10);
        int g = Gvm();
        Check("gvm " + g, g == 87);
        int dl = Delegates();
        Check("delegates " + dl, dl == 931);
        int it = Iterator();
        Check("iterator " + it, it == 44);
        Check("static ctor", StaticCtor() == 99);
        Check("using", Using() == 2);
        string h = Helpers(0, new object(), new int[3]);
        Check("helpers " + h, h == "div ovf idx cast fmt key");
        int rf = ReturnInFinally();
        Check("loop finally " + rf, rf == 312);
        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
