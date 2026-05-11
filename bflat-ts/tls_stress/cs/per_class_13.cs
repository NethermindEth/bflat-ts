        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_13 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_13.V = 13;
                Slot_13.L = 13L * 100L;
                if (Slot_13.V != 13) return 1;
                if (Slot_13.L != 1300L) return 1;
                Console.WriteLine("tls_stress: per_class_13 ok");
                return 0;
            }
        }
