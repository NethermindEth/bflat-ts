        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_36 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_36.V = 36;
                Slot_36.L = 36L * 100L;
                if (Slot_36.V != 36) return 1;
                if (Slot_36.L != 3600L) return 1;
                Console.WriteLine("tls_stress: per_class_36 ok");
                return 0;
            }
        }
