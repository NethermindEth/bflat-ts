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
class C21 : I { public int F() => 21; }
class C22 : I { public int F() => 22; }
class C23 : I { public int F() => 23; }
class C24 : I { public int F() => 24; }
class C25 : I { public int F() => 25; }
class C26 : I { public int F() => 26; }
class C27 : I { public int F() => 27; }
class C28 : I { public int F() => 28; }
class C29 : I { public int F() => 29; }
class C30 : I { public int F() => 30; }
class C31 : I { public int F() => 31; }
class C32 : I { public int F() => 32; }
class C33 : I { public int F() => 33; }
class C34 : I { public int F() => 34; }
class C35 : I { public int F() => 35; }
class C36 : I { public int F() => 36; }
class C37 : I { public int F() => 37; }
class C38 : I { public int F() => 38; }
class C39 : I { public int F() => 39; }
class C40 : I { public int F() => 40; }
class C41 : I { public int F() => 41; }
class C42 : I { public int F() => 42; }
class C43 : I { public int F() => 43; }
class C44 : I { public int F() => 44; }
class C45 : I { public int F() => 45; }
class C46 : I { public int F() => 46; }
class C47 : I { public int F() => 47; }
class C48 : I { public int F() => 48; }
class C49 : I { public int F() => 49; }
class C50 : I { public int F() => 50; }

class Program
{
    static int Main()
    {
        I[] arr = new I[] { new C1(), new C2(), new C3(), new C4(), new C5(), new C6(), new C7(), new C8(), new C9(), new C10(), new C11(), new C12(), new C13(), new C14(), new C15(), new C16(), new C17(), new C18(), new C19(), new C20(), new C21(), new C22(), new C23(), new C24(), new C25(), new C26(), new C27(), new C28(), new C29(), new C30(), new C31(), new C32(), new C33(), new C34(), new C35(), new C36(), new C37(), new C38(), new C39(), new C40(), new C41(), new C42(), new C43(), new C44(), new C45(), new C46(), new C47(), new C48(), new C49(), new C50() };
        int s = 0; foreach (var i in arr) s += i.F();
        if (s != 1275) return 1;
        Console.WriteLine("dispatch: interface_array_50 ok");
        return 0;
    }
}
