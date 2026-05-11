        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_51 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_51.V = 51;
                Slot_51.L = 51L * 100L;
                if (Slot_51.V != 51) return 1;
                if (Slot_51.L != 5100L) return 1;
                Console.WriteLine("tls_stress: per_class_51 ok");
                return 0;
            }
        }
