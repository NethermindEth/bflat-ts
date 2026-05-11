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
        float fz4 = Holder.Fa + Holder.Fb; GC.KeepAlive(fz4);
        double dz4 = Holder.Da * Holder.Db; GC.KeepAlive(dz4);
        float fz5 = Holder.Fa + Holder.Fb; GC.KeepAlive(fz5);
        double dz5 = Holder.Da * Holder.Db; GC.KeepAlive(dz5);
        float fz6 = Holder.Fa + Holder.Fb; GC.KeepAlive(fz6);
        double dz6 = Holder.Da * Holder.Db; GC.KeepAlive(dz6);
        float fz7 = Holder.Fa + Holder.Fb; GC.KeepAlive(fz7);
        double dz7 = Holder.Da * Holder.Db; GC.KeepAlive(dz7);
        Console.WriteLine("nofp: mixed_fp_chain_08 ok");
        return 0;
    }
}
