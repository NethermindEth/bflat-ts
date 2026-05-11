        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_53 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_53.V = 53;
                Slot_53.L = 53L * 100L;
                if (Slot_53.V != 53) return 1;
                if (Slot_53.L != 5300L) return 1;
                Console.WriteLine("tls_stress: per_class_53 ok");
                return 0;
            }
        }
