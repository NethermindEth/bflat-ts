        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_10 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_10.V = 10;
                Slot_10.L = 10L * 100L;
                if (Slot_10.V != 10) return 1;
                if (Slot_10.L != 1000L) return 1;
                Console.WriteLine("tls_stress: per_class_10 ok");
                return 0;
            }
        }
