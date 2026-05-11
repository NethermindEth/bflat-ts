        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class C0 { [ThreadStatic] public static long V; }
class C1 { [ThreadStatic] public static long V; }
class C2 { [ThreadStatic] public static long V; }
class C3 { [ThreadStatic] public static long V; }
class C4 { [ThreadStatic] public static long V; }
class C5 { [ThreadStatic] public static long V; }
class C6 { [ThreadStatic] public static long V; }
class C7 { [ThreadStatic] public static long V; }
class C8 { [ThreadStatic] public static long V; }
class C9 { [ThreadStatic] public static long V; }
class C10 { [ThreadStatic] public static long V; }
class C11 { [ThreadStatic] public static long V; }
class C12 { [ThreadStatic] public static long V; }
class C13 { [ThreadStatic] public static long V; }
class C14 { [ThreadStatic] public static long V; }
class C15 { [ThreadStatic] public static long V; }

        class Program
        {
            static int Main()
            {
                const long iters = 100;
                for (long it = 0; it < iters; it++)
                {
                    C0.V += 1;
            C1.V += 1;
            C2.V += 1;
            C3.V += 1;
            C4.V += 1;
            C5.V += 1;
            C6.V += 1;
            C7.V += 1;
            C8.V += 1;
            C9.V += 1;
            C10.V += 1;
            C11.V += 1;
            C12.V += 1;
            C13.V += 1;
            C14.V += 1;
            C15.V += 1;
                }
                if (C0.V != iters) return 1;
        if (C1.V != iters) return 1;
        if (C2.V != iters) return 1;
        if (C3.V != iters) return 1;
        if (C4.V != iters) return 1;
        if (C5.V != iters) return 1;
        if (C6.V != iters) return 1;
        if (C7.V != iters) return 1;
        if (C8.V != iters) return 1;
        if (C9.V != iters) return 1;
        if (C10.V != iters) return 1;
        if (C11.V != iters) return 1;
        if (C12.V != iters) return 1;
        if (C13.V != iters) return 1;
        if (C14.V != iters) return 1;
        if (C15.V != iters) return 1;
                Console.WriteLine("tls_stress: multi_inc_16 ok");
                return 0;
            }
        }
