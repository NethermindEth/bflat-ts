        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_20 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_20.V = 20;
                Slot_20.L = 20L * 100L;
                if (Slot_20.V != 20) return 1;
                if (Slot_20.L != 2000L) return 1;
                Console.WriteLine("tls_stress: per_class_20 ok");
                return 0;
            }
        }
