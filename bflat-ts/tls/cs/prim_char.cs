        // Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Holder { [ThreadStatic] public static char V; }

        class Program
        {
            static int Main()
            {
                if (!Holder.V.Equals((char)(char)0)) return 1;
                Holder.V = 'A';
                if (!Holder.V.Equals('A')) return 1;
                Console.WriteLine("tls: prim_char ok");
                return 0;
            }
        }
