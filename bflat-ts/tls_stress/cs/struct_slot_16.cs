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
class P8 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P9 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P10 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P11 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P12 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P13 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P14 { public struct V { public int A, B; } [ThreadStatic] public static V S; }
class P15 { public struct V { public int A, B; } [ThreadStatic] public static V S; }

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
        P8.S = new P8.V { A = 8, B = 9 };
        P9.S = new P9.V { A = 9, B = 10 };
        P10.S = new P10.V { A = 10, B = 11 };
        P11.S = new P11.V { A = 11, B = 12 };
        P12.S = new P12.V { A = 12, B = 13 };
        P13.S = new P13.V { A = 13, B = 14 };
        P14.S = new P14.V { A = 14, B = 15 };
        P15.S = new P15.V { A = 15, B = 16 };
                if (P0.S.A != 0 || P0.S.B != 1) return 1;
        if (P1.S.A != 1 || P1.S.B != 2) return 1;
        if (P2.S.A != 2 || P2.S.B != 3) return 1;
        if (P3.S.A != 3 || P3.S.B != 4) return 1;
        if (P4.S.A != 4 || P4.S.B != 5) return 1;
        if (P5.S.A != 5 || P5.S.B != 6) return 1;
        if (P6.S.A != 6 || P6.S.B != 7) return 1;
        if (P7.S.A != 7 || P7.S.B != 8) return 1;
        if (P8.S.A != 8 || P8.S.B != 9) return 1;
        if (P9.S.A != 9 || P9.S.B != 10) return 1;
        if (P10.S.A != 10 || P10.S.B != 11) return 1;
        if (P11.S.A != 11 || P11.S.B != 12) return 1;
        if (P12.S.A != 12 || P12.S.B != 13) return 1;
        if (P13.S.A != 13 || P13.S.B != 14) return 1;
        if (P14.S.A != 14 || P14.S.B != 15) return 1;
        if (P15.S.A != 15 || P15.S.B != 16) return 1;
                Console.WriteLine("tls_stress: struct_slot_16 ok");
                return 0;
            }
        }
