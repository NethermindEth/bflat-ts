        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_44 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_44.V = 44;
                Slot_44.L = 44L * 100L;
                if (Slot_44.V != 44) return 1;
                if (Slot_44.L != 4400L) return 1;
                Console.WriteLine("tls_stress: per_class_44 ok");
                return 0;
            }
        }
