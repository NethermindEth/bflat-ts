        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_21 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_21.V = 21;
                Slot_21.L = 21L * 100L;
                if (Slot_21.V != 21) return 1;
                if (Slot_21.L != 2100L) return 1;
                Console.WriteLine("tls_stress: per_class_21 ok");
                return 0;
            }
        }
