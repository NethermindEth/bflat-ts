// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Tests virtual method dispatch (vtable calls), which exercises
// the object method table layout set up by rhp allocation wrappers
// (__wrap_RhpNewFast, __wrap_RhpNewObject).
using System;

class Vehicle
{
    public virtual string Type => "vehicle";
    public virtual int Speed() => 0;
}

class Car : Vehicle
{
    public override string Type => "car";
    public override int Speed() => 120;
}

class Bicycle : Vehicle
{
    public override string Type => "bicycle";
    public override int Speed() => 25;
}

class Truck : Vehicle
{
    public override string Type => "truck";
    public override int Speed() => 90;
}

class Program
{
    static string Describe(Vehicle v) => $"{v.Type}@{v.Speed()}";

    static int Main()
    {
        Vehicle car  = new Car();
        Vehicle bike = new Bicycle();
        Vehicle truck = new Truck();

        if (car.Speed()  != 120) return 1;
        if (bike.Speed() !=  25) return 1;
        if (truck.Speed() != 90) return 1;

        if (car.Type   != "car")     return 1;
        if (bike.Type  != "bicycle") return 1;
        if (truck.Type != "truck")   return 1;

        if (Describe(car)   != "car@120")    return 1;
        if (Describe(bike)  != "bicycle@25") return 1;
        if (Describe(truck) != "truck@90")   return 1;

        // Array of base-class references - each element dispatches via its own vtable
        Vehicle[] fleet = new Vehicle[] { car, bike, truck, new Car() };
        int totalSpeed = 0;
        foreach (Vehicle v in fleet)
            totalSpeed += v.Speed();
        // 120 + 25 + 90 + 120 = 355
        if (totalSpeed != 355) return 1;

        Console.WriteLine(
            $"dispatch: virtual ok {Describe(car)} {Describe(bike)} total={totalSpeed}");
        return 0;
    }
}