// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Objects allocated in a loop that the JIT may place on the stack.
//
// The JIT moves an object that does not escape its method from the heap to
// a slot of the frame. One allocation site has one slot. Upstream refuses to
// do this for a site inside a loop; our runtime allows it, so every
// iteration reuses the slot of the one before. That is right only while an
// object is dead before the next one is made. The cases here are the ones
// where it is not, or where it is easy to get wrong:
//
//   - two objects of one site alive at the same time: the previous and the
//     current one, the first one kept, the best one kept, a chain of them,
//     a rotation of three, a reference to a field of the previous one;
//   - the same for boxed values and for small arrays;
//   - fields that are set on some iterations only: every iteration has to
//     start from zero and must not see the values of the one before;
//   - an object that escapes on some iterations only;
//   - loops of every form: for, while, do, goto, nested, with break and
//     continue, and a loop in a recursive method.
//
// Every case is one generic method compiled twice. With Inline the object
// is made by a plain `new` in the loop, which can go to the stack. With
// Heap it comes from a method that is not inlined, so it escapes and stays
// on the heap. Both have to give the same result, and that result is also
// compared with a constant worked out by hand.
//
// Each case prints one line; the run ends with ALL PASS or a FAIL count, and
// the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;

class Node
{
    public int V;
    public int W;
    public Node Next;
    public string Tag;
}

class Num
{
    public readonly long V;
    public Num(long v) { V = v; }
}

interface IMake
{
    Node Node();
    Num Num(long v);
    int[] Ints();
    object Box(int v);
}

// A plain `new` at the place of the call.
struct Inline : IMake
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public Node Node() => new Node();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public Num Num(long v) => new Num(v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int[] Ints() => new int[4];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public object Box(int v) => v;
}

// The object comes out of a call, so it is on the heap.
struct Heap : IMake
{
    [MethodImpl(MethodImplOptions.NoInlining)] public Node Node() => new Node();
    [MethodImpl(MethodImplOptions.NoInlining)] public Num Num(long v) => new Num(v);
    [MethodImpl(MethodImplOptions.NoInlining)] public int[] Ints() => new int[4];
    [MethodImpl(MethodImplOptions.NoInlining)] public object Box(int v) => v;
}

static class Cases<M> where M : struct, IMake
{
    static Node kept;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Opaque(int x) => x;

    // One object per iteration, dead at the end of it. The plain case.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long OnePerIteration(int n)
    {
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = Opaque(i);
            cur.W = i * 3;
            sum += cur.V + cur.W;
        }
        return sum;
    }

    // Fields set on even iterations only: an odd one has to read zero.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long FreshFields(int n)
    {
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            if ((Opaque(i) & 1) == 0)
            {
                cur.V = 5;
                cur.Tag = "t";
            }
            sum = sum * 3 + cur.V + (cur.Tag == null ? 0 : 1) + (cur.Next == null ? 0 : 100);
        }
        return sum;
    }

    // The previous object is read after the current one is made.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long PrevAndCur(int n)
    {
        Node prev = null;
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = i + 1;
            if (prev != null) sum += prev.V * 10 + cur.V;
            prev = cur;
        }
        return sum;
    }

    // The same with a call between the two, so they are live across it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long PrevAndCurAcrossCall(int n)
    {
        Node prev = null;
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = Opaque(i + 1);
            if (prev != null) sum += prev.V * 10 + Opaque(cur.V);
            prev = cur;
        }
        return sum;
    }

    // Two objects of one site are never the same object.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Distinct(int n)
    {
        Node prev = null;
        long same = 0;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            if (ReferenceEquals(prev, cur)) same++;
            prev = cur;
        }
        return same;
    }

    // The object of the first iteration is kept to the end.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long KeepFirst(int n)
    {
        Node first = null;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = 100 + i;
            if (first == null) first = cur;
        }
        return first == null ? -1 : first.V;
    }

    // The object with the largest value is kept; it is not the last one.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long KeepBest(int n)
    {
        Node best = null;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = (i * 7) % 11;
            cur.W = i;
            if (best == null || cur.V > best.V) best = cur;
        }
        return best == null ? -1 : best.V * 100 + best.W;
    }

    // A list built in the loop: every object refers to the one before.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Chain(int n)
    {
        Node head = null;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = i + 1;
            cur.Next = head;
            head = cur;
        }
        long sum = 0;
        int steps = 0;
        for (Node p = head; p != null && steps < 1000; p = p.Next, steps++)
            sum = sum * 10 + p.V;
        return sum * 1000 + steps;
    }

    // Three objects alive at a time.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long RotateThree(int n)
    {
        Node a = null, b = null;
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node c = default(M).Node();
            c.V = i + 1;
            if (a != null) sum += a.V * 100 + b.V * 10 + c.V;
            a = b;
            b = c;
        }
        return sum;
    }

    // Each object is made from the two before it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Fib(int n)
    {
        Num a = default(M).Num(0);
        Num b = default(M).Num(1);
        for (int i = 0; i < n; i++)
        {
            Num c = default(M).Num(a.V + b.V);
            a = b;
            b = c;
        }
        return a.V;
    }

    // A reference to a field of the previous object.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long RefToPrevField(int n)
    {
        int none = 0;
        ref int prev = ref none;
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = i + 1;
            sum += prev * 10 + cur.V;
            prev = ref cur.V;
        }
        return sum;
    }

    // Boxed values: the previous box is read after the current one is made.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Boxes(int n)
    {
        object prev = null;
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            object cur = default(M).Box(i + 1);
            if (prev != null) sum += (int)prev * 10 + (int)cur;
            prev = cur;
        }
        return sum;
    }

    // Small arrays of a constant length.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Arrays(int n)
    {
        int[] prev = null;
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            int[] cur = default(M).Ints();
            cur[0] = i + 1;
            if ((i & 1) == 0) cur[3] = 7;
            if (prev != null) sum += prev[0] * 100 + cur[0] * 10 + cur[3] + prev[3];
            prev = cur;
        }
        return sum;
    }

    // Objects stored in an array that does not escape either.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Slots(int n)
    {
        Node[] slots = new Node[4];
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = i + 1;
            slots[i & 3] = cur;
        }
        long sum = 0;
        for (int i = 0; i < 4; i++)
            sum = sum * 10 + (slots[i] == null ? 0 : slots[i].V);
        return sum;
    }

    // The object of one iteration is stored in a static field.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long EscapesOnce(int n, int at)
    {
        kept = null;
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = i + 1;
            if (i == at) kept = cur;
            sum += cur.V;
        }
        return sum * 100 + (kept == null ? 0 : kept.V);
    }

    // The object is kept only on some iterations, and read on the others.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long KeptSometimes(int n)
    {
        Node last = null;
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = i + 1;
            if (i % 3 == 0) last = cur;
            else sum = sum * 10 + last.V;
        }
        return sum;
    }

    // An object of the outer loop is alive while the inner loop makes more.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Nested(int n, int m)
    {
        long sum = 0;
        Node outerPrev = null;
        for (int i = 0; i < n; i++)
        {
            Node outer = default(M).Node();
            outer.V = i + 1;
            Node innerPrev = null;
            for (int j = 0; j < m; j++)
            {
                Node inner = default(M).Node();
                inner.V = (i + 1) * 10 + j;
                if (innerPrev != null) sum += innerPrev.V - inner.V + outer.V;
                innerPrev = inner;
            }
            if (outerPrev != null) sum += outerPrev.V * 1000 + innerPrev.V;
            outerPrev = outer;
        }
        return sum;
    }

    // while with continue and break.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long WhileBreak(int n)
    {
        Node prev = null;
        long sum = 0;
        int i = 0;
        while (true)
        {
            i++;
            Node cur = default(M).Node();
            cur.V = i;
            if ((i & 3) == 0) continue;
            if (prev != null) sum += prev.V * 10 + cur.V;
            prev = cur;
            if (i >= n) break;
        }
        return sum;
    }

    // do-while: the body runs before the first test.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long DoWhile(int n)
    {
        Node prev = null;
        long sum = 0;
        int i = 0;
        do
        {
            Node cur = default(M).Node();
            cur.V = i + 1;
            if (prev != null) sum += prev.V * 10 + cur.V;
            prev = cur;
            i++;
        } while (i < n);
        return sum;
    }

    // A loop made of a backward goto.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Goto(int n)
    {
        Node prev = null;
        long sum = 0;
        int i = 0;
    again:
        Node cur = default(M).Node();
        cur.V = i + 1;
        if (prev != null) sum += prev.V * 10 + cur.V;
        prev = cur;
        i++;
        if (i < n) goto again;
        return sum;
    }

    // A loop in a recursive method: every frame has its own objects.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Recursive(int depth, int n)
    {
        Node prev = null;
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node cur = default(M).Node();
            cur.V = depth * 10 + i;
            if (i == 1 && depth > 0) sum += Recursive(depth - 1, n);
            if (prev != null) sum += prev.V * 3 + cur.V;
            prev = cur;
        }
        return sum;
    }

    // Two sites in one loop, each with a previous object of its own.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long TwoSites(int n)
    {
        Node prevA = null, prevB = null;
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            Node a = default(M).Node();
            a.V = i + 1;
            Node b = default(M).Node();
            b.V = (i + 1) * 2;
            if (prevA != null) sum += prevA.V * 1000 + prevB.V * 100 + a.V * 10 + b.V;
            prevA = a;
            prevB = b;
        }
        return sum;
    }
}

class Program
{
    static int fails;

    static void Check(string name, long inline, long heap, long want)
    {
        bool ok = inline == want && heap == want;
        Console.WriteLine((ok ? "ok   " : "FAIL ") + name +
                          (ok ? "" : " stack=" + inline + " heap=" + heap + " want=" + want));
        if (!ok) fails++;
    }

    static int Main()
    {
        Check("one per iteration", Cases<Inline>.OnePerIteration(10), Cases<Heap>.OnePerIteration(10), 180);
        Check("fresh fields", Cases<Inline>.FreshFields(6), Cases<Heap>.FreshFields(6), 1638);
        Check("previous and current", Cases<Inline>.PrevAndCur(4), Cases<Heap>.PrevAndCur(4), 69);
        Check("previous and current, long", Cases<Inline>.PrevAndCur(100), Cases<Heap>.PrevAndCur(100), 54549);
        Check("previous and current across a call",
              Cases<Inline>.PrevAndCurAcrossCall(4), Cases<Heap>.PrevAndCurAcrossCall(4), 69);
        Check("distinct objects", Cases<Inline>.Distinct(8), Cases<Heap>.Distinct(8), 0);
        Check("keep first", Cases<Inline>.KeepFirst(5), Cases<Heap>.KeepFirst(5), 100);
        Check("keep best", Cases<Inline>.KeepBest(8), Cases<Heap>.KeepBest(8), 1003);
        Check("chain", Cases<Inline>.Chain(5), Cases<Heap>.Chain(5), 54321005);
        Check("rotate three", Cases<Inline>.RotateThree(5), Cases<Heap>.RotateThree(5), 123 + 234 + 345);
        Check("fib", Cases<Inline>.Fib(50), Cases<Heap>.Fib(50), 12586269025);
        Check("ref to previous field", Cases<Inline>.RefToPrevField(4), Cases<Heap>.RefToPrevField(4), 70);
        Check("boxes", Cases<Inline>.Boxes(4), Cases<Heap>.Boxes(4), 69);
        Check("arrays", Cases<Inline>.Arrays(4), Cases<Heap>.Arrays(4), 127 + 237 + 347);
        Check("slots", Cases<Inline>.Slots(6), Cases<Heap>.Slots(6), 5634);
        Check("escapes once", Cases<Inline>.EscapesOnce(5, 2), Cases<Heap>.EscapesOnce(5, 2), 1503);
        Check("escapes never", Cases<Inline>.EscapesOnce(5, 9), Cases<Heap>.EscapesOnce(5, 9), 1500);
        Check("kept sometimes", Cases<Inline>.KeptSometimes(6), Cases<Heap>.KeptSometimes(6), 1144);
        Check("nested", Cases<Inline>.Nested(3, 3), Cases<Heap>.Nested(3, 3), 1024 + 2036);
        Check("while with break", Cases<Inline>.WhileBreak(7), Cases<Heap>.WhileBreak(7), 12 + 23 + 35 + 56 + 67);
        Check("do while", Cases<Inline>.DoWhile(4), Cases<Heap>.DoWhile(4), 69);
        Check("do while, once", Cases<Inline>.DoWhile(0), Cases<Heap>.DoWhile(0), 0);
        Check("goto", Cases<Inline>.Goto(4), Cases<Heap>.Goto(4), 69);
        Check("recursive", Cases<Inline>.Recursive(2, 3), Cases<Heap>.Recursive(2, 3), 6 + 86 + 166);
        Check("two sites", Cases<Inline>.TwoSites(3), Cases<Heap>.TwoSites(3), 1224 + 2436);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
