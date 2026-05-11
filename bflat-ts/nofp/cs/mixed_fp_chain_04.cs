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
        float fz1 = Holder.Fa + Holder.Fb; GC.KeepAlive(fz1);
        double dz1 = Holder.Da * Holder.Db; GC.KeepAlive(dz1);
        float fz2 = Holder.Fa + Holder.Fb; GC.KeepAlive(fz2);
        double dz2 = Holder.Da * Holder.Db; GC.KeepAlive(dz2);
        float fz3 = Holder.Fa + Holder.Fb; GC.KeepAlive(fz3);
        double dz3 = Holder.Da * Holder.Db; GC.KeepAlive(dz3);
        Console.WriteLine("nofp: mixed_fp_chain_04 ok");
        return 0;
    }
}
