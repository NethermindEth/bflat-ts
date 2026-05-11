        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_59 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_59.V = 59;
                Slot_59.L = 59L * 100L;
                if (Slot_59.V != 59) return 1;
                if (Slot_59.L != 5900L) return 1;
                Console.WriteLine("tls_stress: per_class_59 ok");
                return 0;
            }
        }
