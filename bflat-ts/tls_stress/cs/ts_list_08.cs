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
                if (L0.V == null || L0.V[0] != 0) return 1;
        if (L1.V == null || L1.V[0] != 1) return 1;
        if (L2.V == null || L2.V[0] != 2) return 1;
        if (L3.V == null || L3.V[0] != 3) return 1;
        if (L4.V == null || L4.V[0] != 4) return 1;
        if (L5.V == null || L5.V[0] != 5) return 1;
        if (L6.V == null || L6.V[0] != 6) return 1;
        if (L7.V == null || L7.V[0] != 7) return 1;
                Console.WriteLine("tls_stress: ts_list_8 ok");
                return 0;
            }
        }
