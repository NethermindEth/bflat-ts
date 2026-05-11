        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_40 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_40.V = 40;
                Slot_40.L = 40L * 100L;
                if (Slot_40.V != 40) return 1;
                if (Slot_40.L != 4000L) return 1;
                Console.WriteLine("tls_stress: per_class_40 ok");
                return 0;
            }
        }
