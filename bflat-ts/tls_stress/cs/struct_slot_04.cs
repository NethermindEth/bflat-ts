        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class P0 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P1 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P2 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P3 { public struct V { public int A, B; } [ThreadStatic] public static V S; }

        class Program
        {
            static int Main()
            {
                P0.S = new P0.V { A = 0, B = 1 };
        P1.S = new P1.V { A = 1, B = 2 };
        P2.S = new P2.V { A = 2, B = 3 };
        P3.S = new P3.V { A = 3, B = 4 };
                if (P0.S.A != 0 || P0.S.B != 1) return 1;
        if (P1.S.A != 1 || P1.S.B != 2) return 1;
        if (P2.S.A != 2 || P2.S.B != 3) return 1;
        if (P3.S.A != 3 || P3.S.B != 4) return 1;
                Console.WriteLine("tls_stress: struct_slot_4 ok");
                return 0;
            }
        }
