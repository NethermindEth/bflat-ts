// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C1 : I { public int F() => 1; }
class C2 : I { public int F() => 2; }
class C3 : I { public int F() => 3; }
class C4 : I { public int F() => 4; }
class C5 : I { public int F() => 5; }
class C6 : I { public int F() => 6; }
class C7 : I { public int F() => 7; }
class C8 : I { public int F() => 8; }
class C9 : I { public int F() => 9; }
class C10 : I { public int F() => 10; }
class C11 : I { public int F() => 11; }
class C12 : I { public int F() => 12; }
class C13 : I { public int F() => 13; }
class C14 : I { public int F() => 14; }
class C15 : I { public int F() => 15; }
class C16 : I { public int F() => 16; }
class C17 : I { public int F() => 17; }
class C18 : I { public int F() => 18; }
class C19 : I { public int F() => 19; }
class C20 : I { public int F() => 20; }

class Program
{
    static int Main()
    {
        I[] arr = new I[] { new C1(), new C2(), new C3(), new C4(), new C5(), new C6(), new C7(), new C8(), new C9(), new C10(), new C11(), new C12(), new C13(), new C14(), new C15(), new C16(), new C17(), new C18(), new C19(), new C20() };
        int s = 0; foreach (var i in arr) s += i.F();
        if (s != 210) return 1;
        Console.WriteLine("dispatch: interface_array_20 ok");
        return 0;
    }
}
