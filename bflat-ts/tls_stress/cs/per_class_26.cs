        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_26 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_26.V = 26;
                Slot_26.L = 26L * 100L;
                if (Slot_26.V != 26) return 1;
                if (Slot_26.L != 2600L) return 1;
                Console.WriteLine("tls_stress: per_class_26 ok");
                return 0;
            }
        }
