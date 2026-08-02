// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder {
    public static float Fa = 1.0f; public static float Fb = 2.0f;
    public static double Da = 1.0; public static double Db = 2.0;
}


class Program
{
    static int Main()
    {
        float fz0 = Holder.Fa + Holder.Fb; GC.KeepAlive(fz0);
        double dz0 = Holder.Da * Holder.Db; GC.KeepAlive(dz0);
        Console.WriteLine("nofp: mixed_fp_chain_01 ok");
        return 0;
    }
}
