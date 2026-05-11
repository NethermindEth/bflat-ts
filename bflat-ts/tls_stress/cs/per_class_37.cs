        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_37 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_37.V = 37;
                Slot_37.L = 37L * 100L;
                if (Slot_37.V != 37) return 1;
                if (Slot_37.L != 3700L) return 1;
                Console.WriteLine("tls_stress: per_class_37 ok");
                return 0;
            }
        }
