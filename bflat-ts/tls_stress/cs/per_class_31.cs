        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_31 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_31.V = 31;
                Slot_31.L = 31L * 100L;
                if (Slot_31.V != 31) return 1;
                if (Slot_31.L != 3100L) return 1;
                Console.WriteLine("tls_stress: per_class_31 ok");
                return 0;
            }
        }
