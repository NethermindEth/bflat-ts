        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_34 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_34.V = 34;
                Slot_34.L = 34L * 100L;
                if (Slot_34.V != 34) return 1;
                if (Slot_34.L != 3400L) return 1;
                Console.WriteLine("tls_stress: per_class_34 ok");
                return 0;
            }
        }
