        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_48 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_48.V = 48;
                Slot_48.L = 48L * 100L;
                if (Slot_48.V != 48) return 1;
                if (Slot_48.L != 4800L) return 1;
                Console.WriteLine("tls_stress: per_class_48 ok");
                return 0;
            }
        }
