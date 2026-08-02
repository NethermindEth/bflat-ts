        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_11 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_11.V = 11;
                Slot_11.L = 11L * 100L;
                if (Slot_11.V != 11) return 1;
                if (Slot_11.L != 1100L) return 1;
                Console.WriteLine("tls_stress: per_class_11 ok");
                return 0;
            }
        }
