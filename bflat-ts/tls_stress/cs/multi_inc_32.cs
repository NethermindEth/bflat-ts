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
class C16 { [ThreadStatic] public static long V; }
class C17 { [ThreadStatic] public static long V; }
class C18 { [ThreadStatic] public static long V; }
class C19 { [ThreadStatic] public static long V; }
class C20 { [ThreadStatic] public static long V; }
class C21 { [ThreadStatic] public static long V; }
class C22 { [ThreadStatic] public static long V; }
class C23 { [ThreadStatic] public static long V; }
class C24 { [ThreadStatic] public static long V; }
class C25 { [ThreadStatic] public static long V; }
class C26 { [ThreadStatic] public static long V; }
class C27 { [ThreadStatic] public static long V; }
class C28 { [ThreadStatic] public static long V; }
class C29 { [ThreadStatic] public static long V; }
class C30 { [ThreadStatic] public static long V; }
class C31 { [ThreadStatic] public static long V; }

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
            C16.V += 1;
            C17.V += 1;
            C18.V += 1;
            C19.V += 1;
            C20.V += 1;
            C21.V += 1;
            C22.V += 1;
            C23.V += 1;
            C24.V += 1;
            C25.V += 1;
            C26.V += 1;
            C27.V += 1;
            C28.V += 1;
            C29.V += 1;
            C30.V += 1;
            C31.V += 1;
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
        if (C16.V != iters) return 1;
        if (C17.V != iters) return 1;
        if (C18.V != iters) return 1;
        if (C19.V != iters) return 1;
        if (C20.V != iters) return 1;
        if (C21.V != iters) return 1;
        if (C22.V != iters) return 1;
        if (C23.V != iters) return 1;
        if (C24.V != iters) return 1;
        if (C25.V != iters) return 1;
        if (C26.V != iters) return 1;
        if (C27.V != iters) return 1;
        if (C28.V != iters) return 1;
        if (C29.V != iters) return 1;
        if (C30.V != iters) return 1;
        if (C31.V != iters) return 1;
                Console.WriteLine("tls_stress: multi_inc_32 ok");
                return 0;
            }
        }
