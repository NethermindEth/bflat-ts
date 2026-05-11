        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_28 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_28.V = 28;
                Slot_28.L = 28L * 100L;
                if (Slot_28.V != 28) return 1;
                if (Slot_28.L != 2800L) return 1;
                Console.WriteLine("tls_stress: per_class_28 ok");
                return 0;
            }
        }
