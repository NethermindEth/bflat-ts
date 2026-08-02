        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class P0 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P1 { public struct V { public int A, B; } [ThreadStatic] public static V S; }

        class Program
        {
            static int Main()
            {
                P0.S = new P0.V { A = 0, B = 1 };
        P1.S = new P1.V { A = 1, B = 2 };
                if (P0.S.A != 0 || P0.S.B != 1) return 1;
        if (P1.S.A != 1 || P1.S.B != 2) return 1;
                Console.WriteLine("tls_stress: struct_slot_2 ok");
                return 0;
            }
        }
