        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_39 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_39.V = 39;
                Slot_39.L = 39L * 100L;
                if (Slot_39.V != 39) return 1;
                if (Slot_39.L != 3900L) return 1;
                Console.WriteLine("tls_stress: per_class_39 ok");
                return 0;
            }
        }
