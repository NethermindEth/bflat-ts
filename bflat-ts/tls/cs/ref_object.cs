        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static object V; }

        class Program
        {
            static int Main()
            {
                if (Holder.V != null) return 1;
                Holder.V = new object();
                if (Holder.V == null) return 1;
                Console.WriteLine("tls: ref_object ok");
                return 0;
            }
        }
