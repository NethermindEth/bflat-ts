        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_30 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_30.V = 30;
                Slot_30.L = 30L * 100L;
                if (Slot_30.V != 30) return 1;
                if (Slot_30.L != 3000L) return 1;
                Console.WriteLine("tls_stress: per_class_30 ok");
                return 0;
            }
        }
