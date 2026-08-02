        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_2 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_2.V = 2;
                Slot_2.L = 2L * 100L;
                if (Slot_2.V != 2) return 1;
                if (Slot_2.L != 200L) return 1;
                Console.WriteLine("tls_stress: per_class_02 ok");
                return 0;
            }
        }
