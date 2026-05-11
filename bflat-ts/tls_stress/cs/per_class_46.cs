        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_46 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_46.V = 46;
                Slot_46.L = 46L * 100L;
                if (Slot_46.V != 46) return 1;
                if (Slot_46.L != 4600L) return 1;
                Console.WriteLine("tls_stress: per_class_46 ok");
                return 0;
            }
        }
