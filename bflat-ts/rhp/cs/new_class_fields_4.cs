// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

        class Box {
            public int F0;
public int F1;
public int F2;
public int F3;
        }
        class Program
        {
            static int Main()
            {
                var b = new Box();
                b.F0 = 7;
                if (b.F0 != 7) return 1;
                Console.WriteLine("rhp: new_class_fields_4 ok");
                return 0;
            }
        }
