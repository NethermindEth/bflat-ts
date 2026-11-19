// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Stores of object references into the heap.
//
// A store of a reference into a field, an array element or through a byref
// normally calls a write barrier with the address and the value in two
// fixed registers. A zkVM guest has a collector that never moves or frees
// anything, so the barrier does nothing, and the JIT emits the store as a
// plain `sd` with the operands in whatever registers they are in. What can
// go wrong:
//
//   - the address: a field far from the start of the object, an element at
//     a variable index, an address that is the sum of a base and an index
//     and is no longer forced into a register of its own;
//   - the registers: the value and the address in one register (an object
//     stored into itself), a value that is a constant null, a value that
//     comes straight from a call or from another load, many stores in a row
//     that need more registers than there are free;
//   - a store of the wrong width or to the neighbouring slot: the fields
//     and elements around the one written must keep their values;
//   - the stores that are not a single store: an element store with a type
//     check, a struct with references copied as a whole, an exchange.
//
// Each store is in its own method that is not inlined. The object written
// to has guard values on both sides of every reference; the result is read
// back by methods compiled without optimization and compared by reference.
//
// The collector of a zkVM guest never collects, so nothing here can test
// what a collection would make of a missing barrier.
//
// Each case prints one line on failure; the run ends with ALL PASS or a FAIL
// count, and the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

[StructLayout(LayoutKind.Sequential, Size = 4000)] struct Pad4000 { public long B; }

class Holder
{
    public long G0 = 0x1111111111111111;
    public object A;
    public long G1 = 0x2222222222222222;
    public object B;
    public int G2 = 0x33333333;
    public string S;
    public long G3 = 0x4444444444444444;
    public Holder Self;
    public long G4 = 0x5555555555555555;
}

class FarHolder
{
    public long G0 = 0x1111111111111111;
    public Pad4000 Pad;
    public long G1 = 0x2222222222222222;
    public object Far;
    public long G2 = 0x3333333333333333;
    public object Far2;
    public long G3 = 0x4444444444444444;
}

struct Pair { public object First; public long Mid; public object Second; }
struct Triple { public long Head; public string Text; public object Any; public int Tail; }

class PairHolder
{
    public long G0 = 0x1111111111111111;
    public Pair P;
    public long G1 = 0x2222222222222222;
    public Triple T;
    public long G2 = 0x3333333333333333;
}

class Node { public Node Next; public int V; public Node Prev; }

class Statics
{
    public static long G0 = 0x1111111111111111;
    public static object A;
    public static long G1 = 0x2222222222222222;
    public static string S;
    public static long G2 = 0x3333333333333333;
    [ThreadStatic] public static object T;
}

class GenericStatics<T> { public static T Value; public static long Guard = 0x7777; }

static class Program
{
    static int fails;

    static void Check(string name, long expected, long got)
    {
        if (expected != got)
        {
            fails++;
            Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
        }
    }

    static void Same(string name, object expected, object got)
    {
        if (!ReferenceEquals(expected, got))
        {
            fails++;
            Console.WriteLine("FAIL " + name + " expected the stored reference got " + (got == null ? "null" : "another object"));
        }
    }

    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;
    const MethodImplOptions NoInl = MethodImplOptions.NoInlining;

    // ---- readers, compiled without optimization ------------------------------
    [MethodImpl(NoOpt)] static object ReadA(Holder h) => h.A;
    [MethodImpl(NoOpt)] static object ReadB(Holder h) => h.B;
    [MethodImpl(NoOpt)] static object ReadS(Holder h) => h.S;
    [MethodImpl(NoOpt)] static object ReadSelf(Holder h) => h.Self;
    [MethodImpl(NoOpt)] static long Guards(Holder h) =>
        (h.G0 == 0x1111111111111111 ? 0 : 1) + (h.G1 == 0x2222222222222222 ? 0 : 2) + (h.G2 == 0x33333333 ? 0 : 4) +
        (h.G3 == 0x4444444444444444 ? 0 : 8) + (h.G4 == 0x5555555555555555 ? 0 : 16);
    [MethodImpl(NoOpt)] static object ReadFar(FarHolder h) => h.Far;
    [MethodImpl(NoOpt)] static object ReadFar2(FarHolder h) => h.Far2;
    [MethodImpl(NoOpt)] static long Guards(FarHolder h) =>
        (h.G0 == 0x1111111111111111 ? 0 : 1) + (h.G1 == 0x2222222222222222 ? 0 : 2) + (h.G2 == 0x3333333333333333 ? 0 : 4) +
        (h.G3 == 0x4444444444444444 ? 0 : 8) + (h.Pad.B == 0 ? 0 : 16);
    [MethodImpl(NoOpt)] static long Guards(PairHolder h) =>
        (h.G0 == 0x1111111111111111 ? 0 : 1) + (h.G1 == 0x2222222222222222 ? 0 : 2) + (h.G2 == 0x3333333333333333 ? 0 : 4);
    [MethodImpl(NoOpt)] static object At(object[] a, int i) => a[i];
    [MethodImpl(NoOpt)] static long StaticGuards() =>
        (Statics.G0 == 0x1111111111111111 ? 0 : 1) + (Statics.G1 == 0x2222222222222222 ? 0 : 2) + (Statics.G2 == 0x3333333333333333 ? 0 : 4);

    [MethodImpl(NoInl)] static object Fresh(int v) => new Node { V = v };
    [MethodImpl(NoInl)] static string Text(int v) => new string((char)('a' + v % 26), v % 7 + 1);
    [MethodImpl(NoInl)] static int Opaque(int x) => x;

    // ---- stores into fields ----------------------------------------------------
    [MethodImpl(NoInl)] static void SetA(Holder h, object o) => h.A = o;
    [MethodImpl(NoInl)] static void SetB(Holder h, object o) => h.B = o;
    [MethodImpl(NoInl)] static void SetS(Holder h, string s) => h.S = s;
    [MethodImpl(NoInl)] static void SetNull(Holder h) { h.A = null; h.S = null; }
    [MethodImpl(NoInl)] static void SetSelf(Holder h) => h.Self = h;
    [MethodImpl(NoInl)] static void SetAll(Holder h, object a, object b, string s) { h.S = s; h.B = b; h.A = a; h.Self = h; }
    [MethodImpl(NoInl)] static void SetFromCall(Holder h, int v) { h.A = Fresh(v); h.S = Text(v); h.B = Fresh(v + 1); }
    [MethodImpl(NoInl)] static void SetFromLoad(Holder h, Holder from) { h.A = from.B; h.B = from.A; h.S = from.S; }
    [MethodImpl(NoInl)] static void SwapAB(Holder h) => (h.A, h.B) = (h.B, h.A);
    [MethodImpl(NoInl)] static void SetFar(FarHolder h, object o) => h.Far = o;
    [MethodImpl(NoInl)] static void SetFar2(FarHolder h, object a, object b) { h.Far2 = b; h.Far = a; }
    [MethodImpl(NoInl)] static void SetConditional(Holder h, object o, bool second) { if (second) h.B = o; else h.A = o; }
    [MethodImpl(NoInl)] static void SetNewObject(Holder h, int v) => h.A = new Node { V = v, Next = new Node { V = v + 1 } };
    [MethodImpl(NoInl)] static void SetBoxed(Holder h, long v) => h.A = v;

    // ---- stores through byrefs ---------------------------------------------------
    [MethodImpl(NoInl)] static void SetRef(ref object slot, object o) => slot = o;
    [MethodImpl(NoInl)] static void SetOut(out object slot, object o) => slot = o;
    [MethodImpl(NoInl)] static ref object RefToB(Holder h) => ref h.B;
    [MethodImpl(NoInl)] static void SetChosen(Holder h, ref object local, object o, bool heap)
    {
        ref object r = ref heap ? ref h.A : ref local;
        r = o;
    }
    [MethodImpl(NoInl)] static void SetAdd(ref object first, int i, object o) => Unsafe.Add(ref first, i) = o;

    // ---- stores into array elements ------------------------------------------------
    [MethodImpl(NoInl)] static void SetAt(object[] a, int i, object o) => a[i] = o;
    [MethodImpl(NoInl)] static void SetAt3(object[] a, object o) => a[3] = o;
    [MethodImpl(NoInl)] static void SetAtString(string[] a, int i, string s) => a[i] = s;
    [MethodImpl(NoInl)] static void SetAtNode(Node[] a, int i, Node n) => a[i] = n;
    [MethodImpl(NoInl)] static void SetAtNull(object[] a, int i) => a[i] = null;
    [MethodImpl(NoInl)] static void SetItself(object[] a, int i) => a[i] = a;
    [MethodImpl(NoInl)] static void FillAll(object[] a, object o) { for (int i = 0; i < a.Length; i++) a[i] = o; }
    [MethodImpl(NoInl)] static void FillStrings(string[] a, string o) { for (int i = 0; i < a.Length; i++) a[i] = o; }
    [MethodImpl(NoInl)] static void Reverse(Node[] a) { for (int i = 0, j = a.Length - 1; i < j; i++, j--) (a[i], a[j]) = (a[j], a[i]); }
    [MethodImpl(NoInl)] static void CopyUp(string[] a) { for (int i = a.Length - 1; i > 0; i--) a[i] = a[i - 1]; }
    [MethodImpl(NoInl)] static void SetSpan(Span<object> s, int i, object o) => s[i] = o;
    [MethodImpl(NoInl)] static void SetJagged(object[][] a, int i, int j, object o) => a[i][j] = o;
    [MethodImpl(NoInl)] static void SetTwoIndexed(string[] a, int i, int j, string x, string y) { a[i + j] = x; a[i - j] = y; }

    // ---- structs with references ----------------------------------------------------
    [MethodImpl(NoInl)] static void SetPair(PairHolder h, Pair p) => h.P = p;
    [MethodImpl(NoInl)] static void SetPairFields(PairHolder h, object a, object b) { h.P.Second = b; h.P.First = a; h.P.Mid = 77; }
    [MethodImpl(NoInl)] static void SetTriple(PairHolder h, in Triple t) => h.T = t;
    [MethodImpl(NoInl)] static void SetPairAt(Pair[] a, int i, Pair p) => a[i] = p;
    [MethodImpl(NoInl)] static void SetPairRef(ref Pair slot, object a, object b) => slot = new Pair { First = a, Mid = -3, Second = b };
    [MethodImpl(NoInl)] static void CopyPairs(Pair[] from, Pair[] to) { for (int i = 0; i < from.Length; i++) to[to.Length - 1 - i] = from[i]; }
    [MethodImpl(NoInl)] static object BoxPair(object a, object b) => new Pair { First = a, Mid = 5, Second = b };

    // ---- statics ------------------------------------------------------------------------
    [MethodImpl(NoInl)] static void SetStatics(object a, string s) { Statics.S = s; Statics.A = a; }
    [MethodImpl(NoInl)] static void SetThreadStatic(object a) => Statics.T = a;
    [MethodImpl(NoInl)] static void SetGeneric<T>(T v) => GenericStatics<T>.Value = v;

    // ---- linked nodes ---------------------------------------------------------------------
    [MethodImpl(NoInl)] static void Unlink(Node n) => n.Next = n.Next.Next;
    [MethodImpl(NoInl)] static void LinkBoth(Node a, Node b) { a.Next = b; b.Prev = a; }
    [MethodImpl(NoInl)] static Node ReverseList(Node head)
    {
        Node prev = null;
        while (head != null) { Node next = head.Next; head.Next = prev; head.Prev = next; prev = head; head = next; }
        return prev;
    }

    // ---- exchanges ----------------------------------------------------------------------------
    [MethodImpl(NoInl)] static object Exchange(ref object slot, object o) => Interlocked.Exchange(ref slot, o);
    [MethodImpl(NoInl)] static object CompareExchange(ref object slot, object o, object c) => Interlocked.CompareExchange(ref slot, o, c);
    [MethodImpl(NoInl)] static string ExchangeTyped(ref string slot, string o) => Interlocked.Exchange(ref slot, o);
    [MethodImpl(NoInl)] static void VolatileWrite(ref object slot, object o) => Volatile.Write(ref slot, o);

    // Many stores in one method with many values alive.
    [MethodImpl(NoInl)]
    static long ManyStores(Holder h, FarHolder f, object[] arr, Node[] nodes, int n)
    {
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node a = nodes[i % nodes.Length], b = nodes[(i + 1) % nodes.Length], c = nodes[(i + 2) % nodes.Length];
            h.A = a; h.B = b; f.Far = c; f.Far2 = a;
            arr[i % arr.Length] = b;
            arr[(i + 3) % arr.Length] = c;
            a.Next = b; b.Prev = a; c.Next = a;
            h.Self = (i & 1) == 0 ? h : null;
            Statics.A = c;
            sum += a.V + b.V * 3 + c.V * 5 + ((Node)h.A).V + ((Node)f.Far).V + ((Node)arr[i % arr.Length]).V + a.Next.V + c.Next.V;
        }
        return sum;
    }

    static void CaseFields()
    {
        var h = new Holder();
        object a = Fresh(1), b = Fresh(2);
        string s = Text(3);
        SetA(h, a); Same("field A", a, ReadA(h)); Same("field A neighbours", null, ReadB(h));
        SetB(h, b); Same("field B", b, ReadB(h)); Same("field B keeps A", a, ReadA(h));
        SetS(h, s); Same("field S", s, ReadS(h));
        SetSelf(h); Same("field self", h, ReadSelf(h));
        Check("field guards", 0, Guards(h));
        SwapAB(h); Same("swap A", b, ReadA(h)); Same("swap B", a, ReadB(h));
        SetNull(h); Same("null A", null, ReadA(h)); Same("null S", null, ReadS(h)); Same("null keeps B", a, ReadB(h));
        SetAll(h, a, b, s); Same("all A", a, ReadA(h)); Same("all B", b, ReadB(h)); Same("all S", s, ReadS(h)); Same("all self", h, ReadSelf(h));
        var h2 = new Holder();
        SetFromLoad(h2, h); Same("from load A", b, ReadA(h2)); Same("from load B", a, ReadB(h2)); Same("from load S", s, ReadS(h2));
        SetFromLoad(h2, h2); Same("from own load A", a, ReadA(h2)); Same("from own load B", a, ReadB(h2));
        SetFromCall(h2, 40);
        Check("from call", 40 * 100 + 41, ((Node)ReadA(h2)).V * 100 + ((Node)ReadB(h2)).V);
        Check("from call text", 40 % 7 + 1, ((string)ReadS(h2)).Length);
        SetConditional(h2, a, false); SetConditional(h2, b, true);
        Same("conditional A", a, ReadA(h2)); Same("conditional B", b, ReadB(h2));
        SetNewObject(h2, 7);
        Check("new object", 78, ((Node)ReadA(h2)).V * 10 + ((Node)ReadA(h2)).Next.V);
        SetBoxed(h2, long.MinValue + 5);
        Check("boxed", long.MinValue + 5, (long)ReadA(h2));
        Check("field guards after", 0, Guards(h) + Guards(h2));

        var f = new FarHolder();
        SetFar(f, a); Same("far", a, ReadFar(f)); Same("far neighbour", null, ReadFar2(f));
        SetFar2(f, b, s); Same("far first", b, ReadFar(f)); Same("far second", s, ReadFar2(f));
        Check("far guards", 0, Guards(f));
    }

    static void CaseByrefs()
    {
        var h = new Holder();
        object a = Fresh(1), b = Fresh(2), local = null;
        SetRef(ref h.A, a); Same("ref to field", a, ReadA(h));
        SetRef(ref local, b); Same("ref to local", b, local);
        SetOut(out h.B, b); Same("out to field", b, ReadB(h));
        RefToB(h) = a; Same("returned ref", a, ReadB(h));
        SetChosen(h, ref local, b, true); Same("chosen heap", b, ReadA(h));
        SetChosen(h, ref local, a, false); Same("chosen local", a, local); Same("chosen local keeps heap", b, ReadA(h));
        object[] arr = new object[6];
        SetRef(ref arr[4], a); Same("ref to element", a, At(arr, 4));
        SetAdd(ref arr[0], 2, b); Same("ref plus index", b, At(arr, 2));
        Same("ref plus index neighbours", null, At(arr, 1)); Same("ref plus index neighbours", null, At(arr, 3));
        object[] boxes = new object[1];
        SetRef(ref boxes[0], boxes); Same("ref to own array", boxes, At(boxes, 0));
        Check("byref guards", 0, Guards(h));
    }

    static void CaseElements()
    {
        object[] arr = new object[9];
        object a = Fresh(1), b = Fresh(2);
        for (int i = 0; i < 9; i += 2) SetAt(arr, i, i % 4 == 0 ? a : b);
        for (int i = 0; i < 9; i++) Same("element " + i, (i & 1) != 0 ? null : (i % 4 == 0 ? a : b), At(arr, i));
        SetAt3(arr, b); Same("element constant index", b, At(arr, 3));
        SetAtNull(arr, 4); Same("element null", null, At(arr, 4)); Same("element null neighbour", b, At(arr, 3));
        SetItself(arr, 5); Same("element itself", arr, At(arr, 5));
        Check("element length", 9, arr.Length);

        string[] strings = new string[5];
        string s = Text(4);
        SetAtString(strings, 2, s); Same("string element", s, strings[2]);
        FillStrings(strings, s);
        for (int i = 0; i < 5; i++) Same("string fill " + i, s, strings[i]);
        string[] row = { "a", "b", "c", "d" };
        string first = row[0], second = row[1], third = row[2];
        CopyUp(row);
        Same("copy up 3", third, row[3]); Same("copy up 2", second, row[2]); Same("copy up 1", first, row[1]); Same("copy up 0", first, row[0]);
        SetTwoIndexed(strings, 2, 1, "x", "y");
        Check("two indexed", 1, strings[3] == "x" && strings[1] == "y" && ReferenceEquals(strings[2], s) ? 1 : 0);

        Node[] nodes = new Node[7];
        for (int i = 0; i < 7; i++) SetAtNode(nodes, i, new Node { V = i });
        Reverse(nodes);
        long order = 0;
        for (int i = 0; i < 7; i++) order = order * 10 + nodes[i].V;
        Check("reverse", 6543210, order);

        // A store that needs a type check keeps the check.
        object[] asObjects = strings;
        string got = "nothing";
        try { SetAt(asObjects, 0, a); } catch (Exception e) { got = e.GetType().Name; }
        if (got != "ArrayTypeMismatchException") { fails++; Console.WriteLine("FAIL store Node into string[] expected ArrayTypeMismatchException got " + got); }
        Same("refused store left the element", s, strings[0]);
        got = "nothing";
        try { SetAt(arr, 9, a); } catch (Exception e) { got = e.GetType().Name; }
        if (got != "IndexOutOfRangeException") { fails++; Console.WriteLine("FAIL store past the end expected IndexOutOfRangeException got " + got); }

        FillAll(arr, a);
        for (int i = 0; i < 9; i++) Same("fill " + i, a, At(arr, i));
        SetSpan(arr.AsSpan(2, 4), 3, b); Same("span element", b, At(arr, 5)); Same("span neighbour", a, At(arr, 4)); Same("span neighbour", a, At(arr, 6));
        object[][] jag = { new object[2], new object[3] };
        SetJagged(jag, 1, 2, b); Same("jagged", b, jag[1][2]); Same("jagged neighbour", null, jag[1][1]);
    }

    static void CaseStructs()
    {
        var h = new PairHolder();
        object a = Fresh(1), b = Fresh(2);
        SetPair(h, new Pair { First = a, Mid = 9, Second = b });
        Same("pair first", a, h.P.First); Same("pair second", b, h.P.Second); Check("pair mid", 9, h.P.Mid);
        SetPairFields(h, b, a);
        Same("pair fields first", b, h.P.First); Same("pair fields second", a, h.P.Second); Check("pair fields mid", 77, h.P.Mid);
        var t = new Triple { Head = -1, Text = "t", Any = a, Tail = 0x12345678 };
        SetTriple(h, in t);
        Check("triple", 1, h.T.Head == -1 && h.T.Text == "t" && ReferenceEquals(h.T.Any, a) && h.T.Tail == 0x12345678 ? 1 : 0);
        Check("struct guards", 0, Guards(h));

        Pair[] pairs = new Pair[4];
        SetPairAt(pairs, 2, new Pair { First = a, Mid = 1, Second = b });
        Check("pair element", 1, ReferenceEquals(pairs[2].First, a) && ReferenceEquals(pairs[2].Second, b) && pairs[2].Mid == 1 &&
                                  pairs[1].First == null && pairs[1].Second == null && pairs[3].First == null ? 1 : 0);
        SetPairRef(ref pairs[0], b, a);
        Check("pair by ref", 1, ReferenceEquals(pairs[0].First, b) && ReferenceEquals(pairs[0].Second, a) && pairs[0].Mid == -3 ? 1 : 0);
        Pair[] to = new Pair[4];
        CopyPairs(pairs, to);
        Check("pair copy", 1, ReferenceEquals(to[3].First, b) && ReferenceEquals(to[3].Second, a) && ReferenceEquals(to[1].First, a) &&
                               ReferenceEquals(to[1].Second, b) && to[0].First == null && to[2].Second == null && to[3].Mid == -3 ? 1 : 0);
        Pair boxed = (Pair)BoxPair(a, b);
        Check("pair boxed", 1, ReferenceEquals(boxed.First, a) && ReferenceEquals(boxed.Second, b) && boxed.Mid == 5 ? 1 : 0);
    }

    static void CaseStatics()
    {
        object a = Fresh(1);
        string s = Text(5);
        SetStatics(a, s); Same("static A", a, Statics.A); Same("static S", s, Statics.S);
        SetStatics(null, null); Same("static null", null, Statics.A); Same("static null", null, Statics.S);
        SetThreadStatic(a); Same("thread static", a, Statics.T);
        Check("static guards", 0, StaticGuards());
        SetGeneric<string>(s); SetGeneric<object>(a); SetGeneric<Node>((Node)a); SetGeneric<int[]>(new int[3]);
        Check("generic statics", 1, ReferenceEquals(GenericStatics<string>.Value, s) && ReferenceEquals(GenericStatics<object>.Value, a) &&
                                     ReferenceEquals(GenericStatics<Node>.Value, a) && GenericStatics<int[]>.Value.Length == 3 &&
                                     GenericStatics<string>.Guard == 0x7777 && GenericStatics<object>.Guard == 0x7777 ? 1 : 0);
    }

    static void CaseLinks()
    {
        Node n1 = new Node { V = 1 }, n2 = new Node { V = 2 }, n3 = new Node { V = 3 }, n4 = new Node { V = 4 };
        LinkBoth(n1, n2); LinkBoth(n2, n3); LinkBoth(n3, n4);
        Unlink(n1);
        Check("unlink", 1, ReferenceEquals(n1.Next, n3) && ReferenceEquals(n2.Next, n3) && n1.V == 1 && n1.Prev == null ? 1 : 0);
        LinkBoth(n1, n2);
        Node head = ReverseList(n1);
        long order = 0;
        int steps = 0;
        for (Node p = head; p != null && steps < 10; p = p.Next, steps++) order = order * 10 + p.V;
        Check("reverse list", 4321, order);
        Check("reverse list prev", 1, ReferenceEquals(n1.Prev, n2) && n4.Prev == null && n1.Next == null ? 1 : 0);
        LinkBoth(n4, n4);
        Check("self link", 1, ReferenceEquals(n4.Next, n4) && ReferenceEquals(n4.Prev, n4) && n4.V == 4 ? 1 : 0);
    }

    static void CaseExchanges()
    {
        var h = new Holder();
        object a = Fresh(1), b = Fresh(2);
        Same("exchange old", null, Exchange(ref h.A, a)); Same("exchange new", a, ReadA(h));
        Same("exchange old 2", a, Exchange(ref h.A, b)); Same("exchange new 2", b, ReadA(h));
        Same("compare exchange miss", b, CompareExchange(ref h.A, a, a)); Same("compare exchange miss kept", b, ReadA(h));
        Same("compare exchange hit", b, CompareExchange(ref h.A, a, b)); Same("compare exchange hit stored", a, ReadA(h));
        Same("compare exchange null", null, CompareExchange(ref h.B, b, null)); Same("compare exchange null stored", b, ReadB(h));
        string s = Text(2);
        Same("exchange string old", null, ExchangeTyped(ref h.S, s)); Same("exchange string new", s, ReadS(h));
        VolatileWrite(ref h.B, a); Same("volatile write", a, ReadB(h));
        object[] arr = new object[3];
        Same("exchange element old", null, Exchange(ref arr[1], arr)); Same("exchange element new", arr, At(arr, 1));
        Same("exchange element neighbours", null, At(arr, 0)); Same("exchange element neighbours", null, At(arr, 2));
        Check("exchange guards", 0, Guards(h));
    }

    static void CaseMany()
    {
        var h = new Holder();
        var f = new FarHolder();
        object[] arr = new object[5];
        Node[] nodes = new Node[4];
        for (int i = 0; i < 4; i++) nodes[i] = new Node { V = i + 1 };
        const int N = 50;
        long expected = 0;
        for (int i = 0; i < N; i++)
        {
            long a = i % 4 + 1, b = (i + 1) % 4 + 1, c = (i + 2) % 4 + 1;
            expected += a + b * 3 + c * 5 + a + c + b + b + a;
        }
        Check("many stores", expected, ManyStores(h, f, arr, nodes, N));
        int last = N - 1;
        Same("many stores A", nodes[last % 4], ReadA(h));
        Same("many stores B", nodes[(last + 1) % 4], ReadB(h));
        Same("many stores far", nodes[(last + 2) % 4], ReadFar(f));
        Same("many stores far 2", nodes[last % 4], ReadFar2(f));
        Same("many stores self", null, ReadSelf(h));
        Same("many stores static", nodes[(last + 2) % 4], Statics.A);
        Check("many stores guards", 0, Guards(h) + Guards(f) + StaticGuards());
    }

    static int Main()
    {
        CaseFields();
        CaseByrefs();
        CaseElements();
        CaseStructs();
        CaseStatics();
        CaseLinks();
        CaseExchanges();
        CaseMany();

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
