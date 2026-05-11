// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int[] nums = new int[8];
        for (int i = 0; i < nums.Length; i++) nums[i] = i * i;
        if (nums[7] != 49) return 1;
        string[] words = new string[3] { "a", "b", "c" };
        if (words[1] != "b") return 1;
        Console.WriteLine("rhp: arrays ok");
        return 0;
    }
}
