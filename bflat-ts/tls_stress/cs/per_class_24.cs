        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_24 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_24.V = 24;
                Slot_24.L = 24L * 100L;
                if (Slot_24.V != 24) return 1;
                if (Slot_24.L != 2400L) return 1;
                Console.WriteLine("tls_stress: per_class_24 ok");
                return 0;
            }
        }
