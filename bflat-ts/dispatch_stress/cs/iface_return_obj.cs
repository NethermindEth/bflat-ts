// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface IGet { object Get(); }
class CInt : IGet { public object Get() => 42; }
class CStr : IGet { public object Get() => "x"; }


class Program
{
    static int Main()
    {
        IGet[] arr = new IGet[] { new CInt(), new CStr() };
        foreach (var g in arr) { var o = g.Get(); if (o == null) return 1; }
        Console.WriteLine("dispatch_stress: iface_return_obj ok");
        return 0;
    }
}
