        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_8 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_8.V = 8;
                Slot_8.L = 8L * 100L;
                if (Slot_8.V != 8) return 1;
                if (Slot_8.L != 800L) return 1;
                Console.WriteLine("tls_stress: per_class_08 ok");
                return 0;
            }
        }
