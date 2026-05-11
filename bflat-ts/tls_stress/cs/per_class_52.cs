        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_52 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_52.V = 52;
                Slot_52.L = 52L * 100L;
                if (Slot_52.V != 52) return 1;
                if (Slot_52.L != 5200L) return 1;
                Console.WriteLine("tls_stress: per_class_52 ok");
                return 0;
            }
        }
