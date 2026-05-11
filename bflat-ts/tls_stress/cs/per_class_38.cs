        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_38 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_38.V = 38;
                Slot_38.L = 38L * 100L;
                if (Slot_38.V != 38) return 1;
                if (Slot_38.L != 3800L) return 1;
                Console.WriteLine("tls_stress: per_class_38 ok");
                return 0;
            }
        }
