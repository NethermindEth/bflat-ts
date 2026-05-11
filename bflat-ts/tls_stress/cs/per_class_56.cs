        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_56 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_56.V = 56;
                Slot_56.L = 56L * 100L;
                if (Slot_56.V != 56) return 1;
                if (Slot_56.L != 5600L) return 1;
                Console.WriteLine("tls_stress: per_class_56 ok");
                return 0;
            }
        }
