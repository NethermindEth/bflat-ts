        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_4 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_4.V = 4;
                Slot_4.L = 4L * 100L;
                if (Slot_4.V != 4) return 1;
                if (Slot_4.L != 400L) return 1;
                Console.WriteLine("tls_stress: per_class_04 ok");
                return 0;
            }
        }
