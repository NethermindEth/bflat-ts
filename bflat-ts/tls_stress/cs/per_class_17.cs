        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_17 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_17.V = 17;
                Slot_17.L = 17L * 100L;
                if (Slot_17.V != 17) return 1;
                if (Slot_17.L != 1700L) return 1;
                Console.WriteLine("tls_stress: per_class_17 ok");
                return 0;
            }
        }
