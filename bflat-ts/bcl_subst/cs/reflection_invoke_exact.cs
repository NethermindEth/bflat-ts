// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// ConvertOrWidenPrimitivesEnumsAndPointersIfPossible is removed on zisk;
// reflection invocation with EXACTLY-typed arguments must keep working.
using System;
using System.Reflection;

public class Ops
{
    public static int Add(int a, int b) => a + b;
    public static string Tag(string s) => "<" + s + ">";
}

class Program
{
    static int Main()
    {
        MethodInfo add = typeof(Ops).GetMethod("Add");
        if (add == null) return 1;
        object r = add.Invoke(null, new object[] { 3, 4 });
        if ((int)r != 7) return 2;
        MethodInfo tag = typeof(Ops).GetMethod("Tag");
        object s = tag.Invoke(null, new object[] { "x" });
        if ((string)s != "<x>") return 3;
        Console.WriteLine("bcl_subst: reflection_invoke_exact ok");
        return 0;
    }
}
