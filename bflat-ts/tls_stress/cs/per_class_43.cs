        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_43 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_43.V = 43;
                Slot_43.L = 43L * 100L;
                if (Slot_43.V != 43) return 1;
                if (Slot_43.L != 4300L) return 1;
                Console.WriteLine("tls_stress: per_class_43 ok");
                return 0;
            }
        }
