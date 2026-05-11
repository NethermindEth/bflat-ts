        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_0 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_0.V = 0;
                Slot_0.L = 0L * 100L;
                if (Slot_0.V != 0) return 1;
                if (Slot_0.L != 0L) return 1;
                Console.WriteLine("tls_stress: per_class_00 ok");
                return 0;
            }
        }
