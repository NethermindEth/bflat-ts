        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class L0 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L1 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L2 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L3 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L4 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L5 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L6 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L7 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L8 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L9 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L10 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L11 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L12 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L13 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L14 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }
class L15 { [ThreadStatic] public static System.Collections.Generic.List<int> V; }

        class Program
        {
            static int Main()
            {
                L0.V = new System.Collections.Generic.List<int>{ 0 };
        L1.V = new System.Collections.Generic.List<int>{ 1 };
        L2.V = new System.Collections.Generic.List<int>{ 2 };
        L3.V = new System.Collections.Generic.List<int>{ 3 };
        L4.V = new System.Collections.Generic.List<int>{ 4 };
        L5.V = new System.Collections.Generic.List<int>{ 5 };
        L6.V = new System.Collections.Generic.List<int>{ 6 };
        L7.V = new System.Collections.Generic.List<int>{ 7 };
        L8.V = new System.Collections.Generic.List<int>{ 8 };
        L9.V = new System.Collections.Generic.List<int>{ 9 };
        L10.V = new System.Collections.Generic.List<int>{ 10 };
        L11.V = new System.Collections.Generic.List<int>{ 11 };
        L12.V = new System.Collections.Generic.List<int>{ 12 };
        L13.V = new System.Collections.Generic.List<int>{ 13 };
        L14.V = new System.Collections.Generic.List<int>{ 14 };
        L15.V = new System.Collections.Generic.List<int>{ 15 };
                if (L0.V == null || L0.V[0] != 0) return 1;
        if (L1.V == null || L1.V[0] != 1) return 1;
        if (L2.V == null || L2.V[0] != 2) return 1;
        if (L3.V == null || L3.V[0] != 3) return 1;
        if (L4.V == null || L4.V[0] != 4) return 1;
        if (L5.V == null || L5.V[0] != 5) return 1;
        if (L6.V == null || L6.V[0] != 6) return 1;
        if (L7.V == null || L7.V[0] != 7) return 1;
        if (L8.V == null || L8.V[0] != 8) return 1;
        if (L9.V == null || L9.V[0] != 9) return 1;
        if (L10.V == null || L10.V[0] != 10) return 1;
        if (L11.V == null || L11.V[0] != 11) return 1;
        if (L12.V == null || L12.V[0] != 12) return 1;
        if (L13.V == null || L13.V[0] != 13) return 1;
        if (L14.V == null || L14.V[0] != 14) return 1;
        if (L15.V == null || L15.V[0] != 15) return 1;
                Console.WriteLine("tls_stress: ts_list_16 ok");
                return 0;
            }
        }
