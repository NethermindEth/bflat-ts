// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Tests __wrap_S_P_CoreLib_System_Runtime_TypeCast__CheckCastAny
// via is/as/explicit-cast expressions.
using System;

class Animal
{
    public virtual string Name => "Animal";
}

class Dog : Animal
{
    public override string Name => "Dog";
}

class Cat : Animal
{
    public override string Name => "Cat";
}

class Program
{
    static int Main()
    {
        Animal a = new Dog();

        // 'is' check - positive
        if (a is not Dog) return 1;

        // 'as' cast - succeeds
        Dog d = a as Dog;
        if (d == null) return 1;
        if (d.Name != "Dog") return 1;

        // 'is' check - negative
        if (a is Cat) return 1;

        // 'as' cast - fails gracefully (must return null, not crash)
        Cat c = a as Cat;
        if (c != null) return 1;

        // object array - exercises dispatch for each element
        object[] items = new object[] { new Dog(), new Cat(), new Dog() };
        int dogCount = 0;
        foreach (object item in items)
        {
            if (item is Dog)
                dogCount++;
        }
        if (dogCount != 2) return 1;

        Console.WriteLine($"rhp: typecast ok name={d.Name} dogs={dogCount}");
        return 0;
    }
}