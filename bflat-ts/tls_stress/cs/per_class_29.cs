        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_29 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_29.V = 29;
                Slot_29.L = 29L * 100L;
                if (Slot_29.V != 29) return 1;
                if (Slot_29.L != 2900L) return 1;
                Console.WriteLine("tls_stress: per_class_29 ok");
                return 0;
            }
        }
