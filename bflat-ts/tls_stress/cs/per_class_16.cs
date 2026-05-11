        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_16 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_16.V = 16;
                Slot_16.L = 16L * 100L;
                if (Slot_16.V != 16) return 1;
                if (Slot_16.L != 1600L) return 1;
                Console.WriteLine("tls_stress: per_class_16 ok");
                return 0;
            }
        }
