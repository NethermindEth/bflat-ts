        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_35 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_35.V = 35;
                Slot_35.L = 35L * 100L;
                if (Slot_35.V != 35) return 1;
                if (Slot_35.L != 3500L) return 1;
                Console.WriteLine("tls_stress: per_class_35 ok");
                return 0;
            }
        }
