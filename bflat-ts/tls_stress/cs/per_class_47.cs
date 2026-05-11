        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_47 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_47.V = 47;
                Slot_47.L = 47L * 100L;
                if (Slot_47.V != 47) return 1;
                if (Slot_47.L != 4700L) return 1;
                Console.WriteLine("tls_stress: per_class_47 ok");
                return 0;
            }
        }
