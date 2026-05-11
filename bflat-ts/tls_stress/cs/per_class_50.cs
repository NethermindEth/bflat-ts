        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_50 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_50.V = 50;
                Slot_50.L = 50L * 100L;
                if (Slot_50.V != 50) return 1;
                if (Slot_50.L != 5000L) return 1;
                Console.WriteLine("tls_stress: per_class_50 ok");
                return 0;
            }
        }
