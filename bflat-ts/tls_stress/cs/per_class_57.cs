        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_57 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_57.V = 57;
                Slot_57.L = 57L * 100L;
                if (Slot_57.V != 57) return 1;
                if (Slot_57.L != 5700L) return 1;
                Console.WriteLine("tls_stress: per_class_57 ok");
                return 0;
            }
        }
