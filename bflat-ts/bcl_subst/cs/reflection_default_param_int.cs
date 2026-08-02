// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// ParseConstantNumericValue is removed for boxed float/double metadata
// constants; integer default-parameter values must still be readable.
using System;
using System.Reflection;

public class Defaults
{
    public static int WithDefault(int a, int b = 5) => a + b;
}

class Program
{
    static int Main()
    {
        MethodInfo mi = typeof(Defaults).GetMethod("WithDefault");
        if (mi == null) return 1;
        ParameterInfo p = mi.GetParameters()[1];
        if (!p.HasDefaultValue) return 2;
        if ((int)p.DefaultValue != 5) return 3;
        object r = mi.Invoke(null, new object[] { 10, Type.Missing });
        if ((int)r != 15) return 4;
        Console.WriteLine("bcl_subst: reflection_default_param_int ok");
        return 0;
    }
}
