        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Slot_58 { [ThreadStatic] public static int V; [ThreadStatic] public static long L; }

        class Program
        {
            static int Main()
            {
                Slot_58.V = 58;
                Slot_58.L = 58L * 100L;
                if (Slot_58.V != 58) return 1;
                if (Slot_58.L != 5800L) return 1;
                Console.WriteLine("tls_stress: per_class_58 ok");
                return 0;
            }
        }
