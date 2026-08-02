        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_27 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_27.V = 27;
                Slot_27.L = 27L * 100L;
                if (Slot_27.V != 27) return 1;
                if (Slot_27.L != 2700L) return 1;
                Console.WriteLine("tls_stress: per_class_27 ok");
                return 0;
            }
        }
