        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_5 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_5.V = 5;
                Slot_5.L = 5L * 100L;
                if (Slot_5.V != 5) return 1;
                if (Slot_5.L != 500L) return 1;
                Console.WriteLine("tls_stress: per_class_05 ok");
                return 0;
            }
        }
