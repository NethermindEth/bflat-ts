// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Tests TSS slot overflow: WideClass has 68 [ThreadStatic] int fields (272 bytes).
// The TSS allocator reserves only TSS_SLOT_BYTES=256 per type, so F64..F67
// would overflow into GuardClass's slot, corrupting Sentinel.
// Bug targeted: rhp/module.c TSS_SLOT_BYTES fixed at 256
using System;

class WideClass
{
    [ThreadStatic] public static int F00, F01, F02, F03, F04, F05, F06, F07, F08, F09;
    [ThreadStatic] public static int F10, F11, F12, F13, F14, F15, F16, F17, F18, F19;
    [ThreadStatic] public static int F20, F21, F22, F23, F24, F25, F26, F27, F28, F29;
    [ThreadStatic] public static int F30, F31, F32, F33, F34, F35, F36, F37, F38, F39;
    [ThreadStatic] public static int F40, F41, F42, F43, F44, F45, F46, F47, F48, F49;
    [ThreadStatic] public static int F50, F51, F52, F53, F54, F55, F56, F57, F58, F59;
    [ThreadStatic] public static int F60, F61, F62, F63, F64, F65, F66, F67;
}

class GuardClass
{
    [ThreadStatic] public static int Sentinel;
}

class Program
{
    static int Main()
    {
        const int SentinelValue = unchecked((int)0xABCD_1234);

        // Step 1: set guard sentinel before touching WideClass
        GuardClass.Sentinel = SentinelValue;

        // Step 2: set all 68 fields of WideClass to their index value
        WideClass.F00 =  0; WideClass.F01 =  1; WideClass.F02 =  2; WideClass.F03 =  3;
        WideClass.F04 =  4; WideClass.F05 =  5; WideClass.F06 =  6; WideClass.F07 =  7;
        WideClass.F08 =  8; WideClass.F09 =  9;
        WideClass.F10 = 10; WideClass.F11 = 11; WideClass.F12 = 12; WideClass.F13 = 13;
        WideClass.F14 = 14; WideClass.F15 = 15; WideClass.F16 = 16; WideClass.F17 = 17;
        WideClass.F18 = 18; WideClass.F19 = 19;
        WideClass.F20 = 20; WideClass.F21 = 21; WideClass.F22 = 22; WideClass.F23 = 23;
        WideClass.F24 = 24; WideClass.F25 = 25; WideClass.F26 = 26; WideClass.F27 = 27;
        WideClass.F28 = 28; WideClass.F29 = 29;
        WideClass.F30 = 30; WideClass.F31 = 31; WideClass.F32 = 32; WideClass.F33 = 33;
        WideClass.F34 = 34; WideClass.F35 = 35; WideClass.F36 = 36; WideClass.F37 = 37;
        WideClass.F38 = 38; WideClass.F39 = 39;
        WideClass.F40 = 40; WideClass.F41 = 41; WideClass.F42 = 42; WideClass.F43 = 43;
        WideClass.F44 = 44; WideClass.F45 = 45; WideClass.F46 = 46; WideClass.F47 = 47;
        WideClass.F48 = 48; WideClass.F49 = 49;
        WideClass.F50 = 50; WideClass.F51 = 51; WideClass.F52 = 52; WideClass.F53 = 53;
        WideClass.F54 = 54; WideClass.F55 = 55; WideClass.F56 = 56; WideClass.F57 = 57;
        WideClass.F58 = 58; WideClass.F59 = 59;
        WideClass.F60 = 60; WideClass.F61 = 61; WideClass.F62 = 62; WideClass.F63 = 63;
        WideClass.F64 = 64; WideClass.F65 = 65; WideClass.F66 = 66; WideClass.F67 = 67;

        // Step 3: verify all 68 WideClass fields retained their values
        if (WideClass.F00 !=  0) { Console.WriteLine("tss_wide: FAIL F00"); return 1; }
        if (WideClass.F01 !=  1) { Console.WriteLine("tss_wide: FAIL F01"); return 1; }
        if (WideClass.F02 !=  2) { Console.WriteLine("tss_wide: FAIL F02"); return 1; }
        if (WideClass.F03 !=  3) { Console.WriteLine("tss_wide: FAIL F03"); return 1; }
        if (WideClass.F04 !=  4) { Console.WriteLine("tss_wide: FAIL F04"); return 1; }
        if (WideClass.F05 !=  5) { Console.WriteLine("tss_wide: FAIL F05"); return 1; }
        if (WideClass.F06 !=  6) { Console.WriteLine("tss_wide: FAIL F06"); return 1; }
        if (WideClass.F07 !=  7) { Console.WriteLine("tss_wide: FAIL F07"); return 1; }
        if (WideClass.F08 !=  8) { Console.WriteLine("tss_wide: FAIL F08"); return 1; }
        if (WideClass.F09 !=  9) { Console.WriteLine("tss_wide: FAIL F09"); return 1; }
        if (WideClass.F10 != 10) { Console.WriteLine("tss_wide: FAIL F10"); return 1; }
        if (WideClass.F11 != 11) { Console.WriteLine("tss_wide: FAIL F11"); return 1; }
        if (WideClass.F12 != 12) { Console.WriteLine("tss_wide: FAIL F12"); return 1; }
        if (WideClass.F13 != 13) { Console.WriteLine("tss_wide: FAIL F13"); return 1; }
        if (WideClass.F14 != 14) { Console.WriteLine("tss_wide: FAIL F14"); return 1; }
        if (WideClass.F15 != 15) { Console.WriteLine("tss_wide: FAIL F15"); return 1; }
        if (WideClass.F16 != 16) { Console.WriteLine("tss_wide: FAIL F16"); return 1; }
        if (WideClass.F17 != 17) { Console.WriteLine("tss_wide: FAIL F17"); return 1; }
        if (WideClass.F18 != 18) { Console.WriteLine("tss_wide: FAIL F18"); return 1; }
        if (WideClass.F19 != 19) { Console.WriteLine("tss_wide: FAIL F19"); return 1; }
        if (WideClass.F20 != 20) { Console.WriteLine("tss_wide: FAIL F20"); return 1; }
        if (WideClass.F21 != 21) { Console.WriteLine("tss_wide: FAIL F21"); return 1; }
        if (WideClass.F22 != 22) { Console.WriteLine("tss_wide: FAIL F22"); return 1; }
        if (WideClass.F23 != 23) { Console.WriteLine("tss_wide: FAIL F23"); return 1; }
        if (WideClass.F24 != 24) { Console.WriteLine("tss_wide: FAIL F24"); return 1; }
        if (WideClass.F25 != 25) { Console.WriteLine("tss_wide: FAIL F25"); return 1; }
        if (WideClass.F26 != 26) { Console.WriteLine("tss_wide: FAIL F26"); return 1; }
        if (WideClass.F27 != 27) { Console.WriteLine("tss_wide: FAIL F27"); return 1; }
        if (WideClass.F28 != 28) { Console.WriteLine("tss_wide: FAIL F28"); return 1; }
        if (WideClass.F29 != 29) { Console.WriteLine("tss_wide: FAIL F29"); return 1; }
        if (WideClass.F30 != 30) { Console.WriteLine("tss_wide: FAIL F30"); return 1; }
        if (WideClass.F31 != 31) { Console.WriteLine("tss_wide: FAIL F31"); return 1; }
        if (WideClass.F32 != 32) { Console.WriteLine("tss_wide: FAIL F32"); return 1; }
        if (WideClass.F33 != 33) { Console.WriteLine("tss_wide: FAIL F33"); return 1; }
        if (WideClass.F34 != 34) { Console.WriteLine("tss_wide: FAIL F34"); return 1; }
        if (WideClass.F35 != 35) { Console.WriteLine("tss_wide: FAIL F35"); return 1; }
        if (WideClass.F36 != 36) { Console.WriteLine("tss_wide: FAIL F36"); return 1; }
        if (WideClass.F37 != 37) { Console.WriteLine("tss_wide: FAIL F37"); return 1; }
        if (WideClass.F38 != 38) { Console.WriteLine("tss_wide: FAIL F38"); return 1; }
        if (WideClass.F39 != 39) { Console.WriteLine("tss_wide: FAIL F39"); return 1; }
        if (WideClass.F40 != 40) { Console.WriteLine("tss_wide: FAIL F40"); return 1; }
        if (WideClass.F41 != 41) { Console.WriteLine("tss_wide: FAIL F41"); return 1; }
        if (WideClass.F42 != 42) { Console.WriteLine("tss_wide: FAIL F42"); return 1; }
        if (WideClass.F43 != 43) { Console.WriteLine("tss_wide: FAIL F43"); return 1; }
        if (WideClass.F44 != 44) { Console.WriteLine("tss_wide: FAIL F44"); return 1; }
        if (WideClass.F45 != 45) { Console.WriteLine("tss_wide: FAIL F45"); return 1; }
        if (WideClass.F46 != 46) { Console.WriteLine("tss_wide: FAIL F46"); return 1; }
        if (WideClass.F47 != 47) { Console.WriteLine("tss_wide: FAIL F47"); return 1; }
        if (WideClass.F48 != 48) { Console.WriteLine("tss_wide: FAIL F48"); return 1; }
        if (WideClass.F49 != 49) { Console.WriteLine("tss_wide: FAIL F49"); return 1; }
        if (WideClass.F50 != 50) { Console.WriteLine("tss_wide: FAIL F50"); return 1; }
        if (WideClass.F51 != 51) { Console.WriteLine("tss_wide: FAIL F51"); return 1; }
        if (WideClass.F52 != 52) { Console.WriteLine("tss_wide: FAIL F52"); return 1; }
        if (WideClass.F53 != 53) { Console.WriteLine("tss_wide: FAIL F53"); return 1; }
        if (WideClass.F54 != 54) { Console.WriteLine("tss_wide: FAIL F54"); return 1; }
        if (WideClass.F55 != 55) { Console.WriteLine("tss_wide: FAIL F55"); return 1; }
        if (WideClass.F56 != 56) { Console.WriteLine("tss_wide: FAIL F56"); return 1; }
        if (WideClass.F57 != 57) { Console.WriteLine("tss_wide: FAIL F57"); return 1; }
        if (WideClass.F58 != 58) { Console.WriteLine("tss_wide: FAIL F58"); return 1; }
        if (WideClass.F59 != 59) { Console.WriteLine("tss_wide: FAIL F59"); return 1; }
        if (WideClass.F60 != 60) { Console.WriteLine("tss_wide: FAIL F60"); return 1; }
        if (WideClass.F61 != 61) { Console.WriteLine("tss_wide: FAIL F61"); return 1; }
        if (WideClass.F62 != 62) { Console.WriteLine("tss_wide: FAIL F62"); return 1; }
        if (WideClass.F63 != 63) { Console.WriteLine("tss_wide: FAIL F63"); return 1; }
        if (WideClass.F64 != 64) { Console.WriteLine("tss_wide: FAIL F64"); return 1; }
        if (WideClass.F65 != 65) { Console.WriteLine("tss_wide: FAIL F65"); return 1; }
        if (WideClass.F66 != 66) { Console.WriteLine("tss_wide: FAIL F66"); return 1; }
        if (WideClass.F67 != 67) { Console.WriteLine("tss_wide: FAIL F67"); return 1; }

        // Step 4: guard sentinel must be intact (overflow would have corrupted it)
        if (GuardClass.Sentinel != SentinelValue)
        {
            Console.WriteLine(
                $"tss_wide: FAIL sentinel=0x{GuardClass.Sentinel:X8} expected=0x{SentinelValue:X8}");
            return 1;
        }

        Console.WriteLine("tss_wide: ok 68 fields intact sentinel=0xABCD1234");
        return 0;
    }
}