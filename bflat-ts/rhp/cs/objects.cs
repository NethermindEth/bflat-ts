// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Tests __wrap_RhpNewFast / __wrap_RhpNewObject by allocating
// instances of various class types and verifying field values.
using System;

class Point
{
    public int X;
    public int Y;
    public Point(int x, int y) { X = x; Y = y; }
}

class Box
{
    public string Label;
    public int Size;
    public Box(string label, int size) { Label = label; Size = size; }
}

class Node
{
    public int Value;
    public Node Next;
    public Node(int value, Node next = null) { Value = value; Next = next; }
}

class Program
{
    static int Main()
    {
        // Basic two-field object
        var p = new Point(3, 4);
        if (p.X != 3 || p.Y != 4)
        {
            Console.WriteLine($"rhp/objects: Point field mismatch X={p.X} Y={p.Y}");
            return 1;
        }

        // Object with a reference field
        var b = new Box("test", 42);
        if (b.Label != "test" || b.Size != 42)
        {
            Console.WriteLine($"rhp/objects: Box field mismatch Label={b.Label} Size={b.Size}");
            return 1;
        }

        // Linked chain - exercises multiple allocs and reference writes
        var head = new Node(1, new Node(2, new Node(3)));
        if (head.Value != 1 || head.Next.Value != 2 || head.Next.Next.Value != 3)
        {
            Console.WriteLine("rhp/objects: Node chain mismatch");
            return 1;
        }
        if (head.Next.Next.Next != null)
        {
            Console.WriteLine("rhp/objects: Node chain tail not null");
            return 1;
        }

        Console.WriteLine($"rhp/objects: ok p=({p.X},{p.Y}) b=({b.Label},{b.Size}) chain={head.Value}->{head.Next.Value}->{head.Next.Next.Value}");
        return 0;
    }
}