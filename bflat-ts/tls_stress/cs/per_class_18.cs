        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_18 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_18.V = 18;
                Slot_18.L = 18L * 100L;
                if (Slot_18.V != 18) return 1;
                if (Slot_18.L != 1800L) return 1;
                Console.WriteLine("tls_stress: per_class_18 ok");
                return 0;
            }
        }
