// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// `using` must dispose the resource when the body throws.
using System;

class Resource : IDisposable
{
    public static int Disposed;
    public void Dispose() => Disposed++;
}

class Program
{
    static int Main()
    {
        try
        {
            using (var r = new Resource())
            {
                throw new InvalidOperationException("in-using");
            }
        }
        catch (InvalidOperationException)
        {
            if (Resource.Disposed != 1) return 2;
            Console.WriteLine("eh: eh_using_dispose_on_throw ok");
            return 0;
        }
    }
}
