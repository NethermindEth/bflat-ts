        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_32 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_32.V = 32;
                Slot_32.L = 32L * 100L;
                if (Slot_32.V != 32) return 1;
                if (Slot_32.L != 3200L) return 1;
                Console.WriteLine("tls_stress: per_class_32 ok");
                return 0;
            }
        }
