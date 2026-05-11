        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_9 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_9.V = 9;
                Slot_9.L = 9L * 100L;
                if (Slot_9.V != 9) return 1;
                if (Slot_9.L != 900L) return 1;
                Console.WriteLine("tls_stress: per_class_09 ok");
                return 0;
            }
        }
