        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class P0 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P1 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P2 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P3 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P4 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P5 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P6 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P7 { public struct V { public int A, B; } [ThreadStatic] public static V S; }

        class Program
        {
            static int Main()
            {
                P0.S = new P0.V { A = 0, B = 1 };
        P1.S = new P1.V { A = 1, B = 2 };
        P2.S = new P2.V { A = 2, B = 3 };
        P3.S = new P3.V { A = 3, B = 4 };
        P4.S = new P4.V { A = 4, B = 5 };
        P5.S = new P5.V { A = 5, B = 6 };
        P6.S = new P6.V { A = 6, B = 7 };
        P7.S = new P7.V { A = 7, B = 8 };
                if (P0.S.A != 0 || P0.S.B != 1) return 1;
        if (P1.S.A != 1 || P1.S.B != 2) return 1;
        if (P2.S.A != 2 || P2.S.B != 3) return 1;
        if (P3.S.A != 3 || P3.S.B != 4) return 1;
        if (P4.S.A != 4 || P4.S.B != 5) return 1;
        if (P5.S.A != 5 || P5.S.B != 6) return 1;
        if (P6.S.A != 6 || P6.S.B != 7) return 1;
        if (P7.S.A != 7 || P7.S.B != 8) return 1;
                Console.WriteLine("tls_stress: struct_slot_8 ok");
                return 0;
            }
        }
