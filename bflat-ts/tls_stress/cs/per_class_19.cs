        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_19 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_19.V = 19;
                Slot_19.L = 19L * 100L;
                if (Slot_19.V != 19) return 1;
                if (Slot_19.L != 1900L) return 1;
                Console.WriteLine("tls_stress: per_class_19 ok");
                return 0;
            }
        }
