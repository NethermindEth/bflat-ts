// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Virtual and interface calls that the JIT turns into a direct call because
// every class the receiver can have resolves the call to one method.
//
// The compiler asks for the list of classes that implement an interface or
// derive from a class, resolves the method on each, and when all agree it
// calls that method directly, with no type test. That is right only when
// the list is complete. What can go wrong:
//
//   - a receiver class that is not in the list: an array used through
//     IReadOnlyList<T> / IList<T> / IEnumerable<T>, a class reached only
//     through interface variance, an object made by Activator, by `new T()`
//     or by GetUninitializedObject, an IDynamicInterfaceCastable object;
//   - a class in the list that resolves differently and is missed: one leaf
//     that overrides, a class that implements the interface again, a
//     default interface method that one class replaces;
//   - the null check: the direct call does not load the method table, so a
//     null receiver must still throw NullReferenceException, also when the
//     target never touches `this` and when it is inlined;
//   - the context of the target: generic base classes, shared code, a struct
//     among the implementors.
//
// Each hierarchy has more subclasses than a guarded devirtualization tests.
// Every call site is its own method that is not inlined. The expected value
// of each call is written next to the object when it is made and does not
// go through the call under test.
//
// Each case prints one line on failure; the run ends with ALL PASS or a FAIL
// count, and the guest returns the number of failures.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ---- A: an interface implemented once, at the root -------------------------
interface ISpec { int Id(); long Scale(long x); bool IsOn { get; } int Const(); }
class SpecBase : ISpec
{
    public int K;
    public int Id() => K * 3 + 1;
    public long Scale(long x) => x * K + 7;
    public bool IsOn => (K & 1) != 0;
    public int Const() => 42;                 // does not touch `this`
}
class Spec1 : SpecBase { } class Spec2 : SpecBase { } class Spec3 : SpecBase { }
class Spec4 : SpecBase { } class Spec5 : SpecBase { } class Spec6 : SpecBase { public int Extra = 9; }

// ---- B: one class implements the interface again ----------------------------
interface IRe { int F(); }
class ReBase : IRe { public int F() => 10; }
class Re1 : ReBase { } class Re2 : ReBase { } class Re3 : ReBase { } class Re4 : ReBase { } class Re5 : ReBase { }
class ReAgain : ReBase, IRe { int IRe.F() => 20; }

// ---- C: a virtual method that one leaf overrides, and one that none does ----
class VBase { public int K; public virtual int F() => K + 100; public virtual int G() => K + 200; }
class V1 : VBase { } class V2 : VBase { } class V3 : VBase { } class V4 : VBase { } class V5 : VBase { }
class VOver : VBase { public override int F() => K + 1000; }

// ---- D: the overriding class is never made by a plain `new` -----------------
interface IAct { int F(); }
class ActBase : IAct { public virtual int F() => 1; }
class Act1 : ActBase { } class Act2 : ActBase { } class Act3 : ActBase { } class Act4 : ActBase { } class Act5 : ActBase { }
class ActGeneric : ActBase { public override int F() => 2; }      // Activator.CreateInstance<T>()
class ActNewT : ActBase { public override int F() => 3; }         // new T()
class ActType : ActBase { public override int F() => 4; }         // Activator.CreateInstance(Type)
class ActUninit : ActBase { public override int F() => 5; }       // GetUninitializedObject
class ActClone : ActBase { public override int F() => 6; public object Clone() => MemberwiseClone(); }

// ---- E: arrays and variance -------------------------------------------------
struct Elem { public int V; }
class Item { public int V; }
class SubItem : Item { }
class ElemList : IReadOnlyList<Elem>, IList<Elem>
{
    public Elem[] A = new Elem[3];
    public Elem this[int i] { get => new Elem { V = -1 - i }; set { } }
    public int Count => 77;
    public bool IsReadOnly => true;
    public IEnumerator<Elem> GetEnumerator() => throw new NotSupportedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
    public void Add(Elem e) { } public void Clear() { } public bool Contains(Elem e) => false;
    public void CopyTo(Elem[] a, int i) { } public int IndexOf(Elem e) => -5; public void Insert(int i, Elem e) { }
    public bool Remove(Elem e) => false; public void RemoveAt(int i) { }
}
class ItemList : IReadOnlyList<Item>
{
    public static readonly Item Fixed = new Item { V = -9 };
    public Item this[int i] => Fixed;
    public int Count => 55;
    public IEnumerator<Item> GetEnumerator() => throw new NotSupportedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
}
class SubItemList : IReadOnlyList<SubItem>
{
    public static readonly SubItem Fixed = new SubItem { V = -19 };
    public SubItem this[int i] => Fixed;
    public int Count => 66;
    public IEnumerator<SubItem> GetEnumerator() => throw new NotSupportedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
}
class Animal { public virtual int Legs => 0; }
class Dog : Animal { public override int Legs => 4; }
interface IProducer<out T> { T Make(); int Tag(); }
interface IConsumer<in T> { int Eat(T t); }
class AnimalProducer : IProducer<Animal> { public Animal Make() => new Animal(); public int Tag() => 1; }
class AnimalProducer2 : AnimalProducer { } class AnimalProducer3 : AnimalProducer { }
class AnimalProducer4 : AnimalProducer { } class AnimalProducer5 : AnimalProducer { }
class DogProducer : IProducer<Dog> { public Dog Make() => new Dog(); public int Tag() => 2; }
class DogConsumer : IConsumer<Dog> { public int Eat(Dog d) => 10 + d.Legs; }
class DogConsumer2 : DogConsumer { } class DogConsumer3 : DogConsumer { }
class DogConsumer4 : DogConsumer { } class DogConsumer5 : DogConsumer { }
class AnimalConsumer : IConsumer<Animal> { public int Eat(Animal a) => 20 + a.Legs; }

// ---- F: default interface methods -------------------------------------------
interface IDim { int F() => 100 + G(); int G(); }
class Dim1 : IDim { public int G() => 1; } class Dim2 : IDim { public int G() => 2; }
class Dim3 : IDim { public int G() => 3; } class Dim4 : IDim { public int G() => 4; }
class Dim5 : IDim { public int G() => 5; } class Dim6 : Dim5 { }
interface IDimO { int F() => 100 + G(); int G(); }
class DimO1 : IDimO { public int G() => 1; } class DimO2 : IDimO { public int G() => 2; }
class DimO3 : IDimO { public int G() => 3; } class DimO4 : IDimO { public int G() => 4; }
class DimO5 : IDimO { public int G() => 5; }
class DimOver : IDimO { public int G() => 6; int IDimO.F() => 600; }
interface IDimDerived : IDimO { int IDimO.F() => 700 + G(); }
class DimViaDerived : IDimDerived { public int G() => 7; }

// ---- G: generic base classes ------------------------------------------------
interface IG<T> { T Get(); int Size(); }
class GBase<T> : IG<T> { public T V; public T Get() => V; public int Size() => Unsafe.SizeOf<T>(); }
class GI1 : GBase<int> { } class GI2 : GBase<int> { } class GI3 : GBase<int> { }
class GI4 : GBase<int> { } class GI5 : GBase<int> { }
class GS1 : GBase<string> { } class GS2 : GBase<string> { } class GS3 : GBase<string> { }
class GS4 : GBase<string> { } class GS5 : GBase<string> { }
class GO1 : GBase<object> { }
class GL1<T> : GBase<long> { public T Pad; }

// ---- H: abstract root, one implementation in the middle ---------------------
abstract class HRoot { public int K; public abstract int F(); public int Twice() => F() * 2; }
class HMid : HRoot { public override int F() => K + 5; }
class H1 : HMid { } class H2 : HMid { } class H3 : HMid { } class H4 : HMid { } class H5 : HMid { }
abstract class HRoot2 { public int K; public abstract int F(); }
class HMid2 : HRoot2 { public override int F() => K + 5; }
class H21 : HMid2 { } class H22 : HMid2 { } class H23 : HMid2 { } class H24 : HMid2 { }
sealed class H2Leaf : HMid2 { public override int F() => K + 50; }

// ---- I: a struct among the implementors -------------------------------------
interface IMix { int F(); }
class MixBase : IMix { public int F() => 1; }
class Mix1 : MixBase { } class Mix2 : MixBase { } class Mix3 : MixBase { } class Mix4 : MixBase { }
struct MixStruct : IMix { public int V; public int F() => 1000 + V; }

// ---- K: the unique target calls a virtual itself ----------------------------
interface IK { int F(); }
class KBase : IK { int IK.F() => V() * 10; protected virtual int V() => 1; }
class K1 : KBase { protected override int V() => 2; } class K2 : KBase { protected override int V() => 3; }
class K3 : KBase { } class K4 : KBase { protected override int V() => 5; } class K5 : K4 { }

// ---- L: an inherited interface ----------------------------------------------
interface IL1 { int F(); }
interface IL2 : IL1 { int H(); }
class LBase : IL2 { public int K; public int F() => K + 1; public int H() => K + 2; }
class L1 : LBase { } class L2 : LBase { } class L3 : LBase { } class L4 : LBase { } class L5 : LBase { }
class LOnly1 : IL1 { public int F() => -1; }

// ---- N: hiding with `new` ---------------------------------------------------
interface IN { int F(); }
class NBase : IN { public int F() => 1; }
class N1 : NBase { public new int F() => 2; }            // hides, does not implement again: 1
class N2 : NBase { } class N3 : NBase { } class N4 : NBase { } class N5 : NBase { }
interface IN2 { int F(); }
class NBase2 : IN2 { public int F() => 1; }
class N21 : NBase2 { } class N22 : NBase2 { } class N23 : NBase2 { } class N24 : NBase2 { }
class N2Again : NBase2, IN2 { public new int F() => 3; }  // implements again: 3

// ---- R: IDynamicInterfaceCastable -------------------------------------------
interface IDyn { int F(); }
class DynBase : IDyn { public int F() => 1; }
class Dyn1 : DynBase { } class Dyn2 : DynBase { } class Dyn3 : DynBase { } class Dyn4 : DynBase { }
[DynamicInterfaceCastableImplementation]
interface IDynImpl : IDyn { int IDyn.F() => 77; }
class DynCastable : IDynamicInterfaceCastable
{
    public bool IsInterfaceImplemented(RuntimeTypeHandle t, bool throwIfNotImplemented) => t.Equals(typeof(IDyn).TypeHandle);
    public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle t) => typeof(IDynImpl).TypeHandle;
}

// ---- S: static state of the target's class ----------------------------------
interface ICc { int F(); }
class CcBase : ICc
{
    public static int Inits;
    static readonly int s_v = Init();
    static int Init() { Inits++; return 31; }
    public int F() => s_v;
}
class Cc1 : CcBase { } class Cc2 : CcBase { } class Cc3 : CcBase { } class Cc4 : CcBase { } class Cc5 : CcBase { }

static class Factory<T> where T : new()
{
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Make() => new T();
}

static class Program
{
    static int fails;

    static void Check(string name, long expected, long got)
    {
        if (expected != got)
        {
            fails++;
            Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)] static T Opaque<T>(T x) => x;

    // ---- call sites ---------------------------------------------------------
    [MethodImpl(MethodImplOptions.NoInlining)] static int SpecId(ISpec s) => s.Id();
    [MethodImpl(MethodImplOptions.NoInlining)] static long SpecScale(ISpec s, long x) => s.Scale(x);
    [MethodImpl(MethodImplOptions.NoInlining)] static bool SpecOn(ISpec s) => s.IsOn;
    [MethodImpl(MethodImplOptions.NoInlining)] static int SpecConst(ISpec s) => s.Const();
    [MethodImpl(MethodImplOptions.NoInlining)] static int SpecConstBase(SpecBase s) => s.Const();
    [MethodImpl(MethodImplOptions.NoInlining)] static int SpecLoop(ISpec[] a)
    {
        int sum = 0;
        for (int i = 0; i < a.Length; i++) sum = sum * 31 + a[i].Id() + a[i].Const();
        return sum;
    }
    [MethodImpl(MethodImplOptions.NoInlining)] static int SpecAfterCast(object o) => ((ISpec)o).Id();
    [MethodImpl(MethodImplOptions.NoInlining)] static int SpecAfterIs(object o) => o is ISpec s ? s.Id() : -1;

    [MethodImpl(MethodImplOptions.NoInlining)] static int ReF(IRe r) => r.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int ReBaseF(ReBase r) => ((IRe)r).F();

    [MethodImpl(MethodImplOptions.NoInlining)] static int VF(VBase v) => v.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int VG(VBase v) => v.G();

    [MethodImpl(MethodImplOptions.NoInlining)] static int ActF(IAct a) => a.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int ActBaseF(ActBase a) => a.F();

    [MethodImpl(MethodImplOptions.NoInlining)] static int ElemAt(IReadOnlyList<Elem> l, int i) => l[i].V;
    [MethodImpl(MethodImplOptions.NoInlining)] static int ElemCount(IReadOnlyList<Elem> l) => l.Count;
    [MethodImpl(MethodImplOptions.NoInlining)] static int ElemAtL(IList<Elem> l, int i) => l[i].V;
    [MethodImpl(MethodImplOptions.NoInlining)] static int ElemCountC(ICollection<Elem> l) => l.Count;
    [MethodImpl(MethodImplOptions.NoInlining)] static int ElemIndexOf(IList<Elem> l, Elem e) => l.IndexOf(e);
    [MethodImpl(MethodImplOptions.NoInlining)] static int ItemAt(IReadOnlyList<Item> l, int i) => l[i].V;
    [MethodImpl(MethodImplOptions.NoInlining)] static int ItemCount(IReadOnlyList<Item> l) => l.Count;
    [MethodImpl(MethodImplOptions.NoInlining)] static int ItemCountC(IReadOnlyCollection<Item> l) => l.Count;

    [MethodImpl(MethodImplOptions.NoInlining)] static int ProdTag(IProducer<Animal> p) => p.Tag();
    [MethodImpl(MethodImplOptions.NoInlining)] static int ProdLegs(IProducer<Animal> p) => p.Make().Legs;
    [MethodImpl(MethodImplOptions.NoInlining)] static int ConsEat(IConsumer<Dog> c, Dog d) => c.Eat(d);

    [MethodImpl(MethodImplOptions.NoInlining)] static int DimF(IDim d) => d.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int DimOF(IDimO d) => d.F();

    [MethodImpl(MethodImplOptions.NoInlining)] static int GInt(IG<int> g) => g.Get();
    [MethodImpl(MethodImplOptions.NoInlining)] static int GIntSize(IG<int> g) => g.Size();
    [MethodImpl(MethodImplOptions.NoInlining)] static string GStr(IG<string> g) => g.Get();
    [MethodImpl(MethodImplOptions.NoInlining)] static int GStrSize(IG<string> g) => g.Size();
    [MethodImpl(MethodImplOptions.NoInlining)] static long GLong(IG<long> g) => g.Get();
    [MethodImpl(MethodImplOptions.NoInlining)] static T GShared<T>(IG<T> g) => g.Get();
    [MethodImpl(MethodImplOptions.NoInlining)] static int GSharedSize<T>(IG<T> g) => g.Size();
    [MethodImpl(MethodImplOptions.NoInlining)] static T GBaseShared<T>(GBase<T> g) => g.Get();

    [MethodImpl(MethodImplOptions.NoInlining)] static int HF(HRoot h) => h.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int HTwice(HRoot h) => h.Twice();
    [MethodImpl(MethodImplOptions.NoInlining)] static int H2F(HRoot2 h) => h.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int H2MidF(HMid2 h) => h.F();

    [MethodImpl(MethodImplOptions.NoInlining)] static int MixF(IMix m) => m.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int MixGeneric<T>(T m) where T : IMix => m.F();

    [MethodImpl(MethodImplOptions.NoInlining)] static int KF(IK k) => k.F();

    [MethodImpl(MethodImplOptions.NoInlining)] static int L1F(IL1 l) => l.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int L2F(IL2 l) => l.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int L2H(IL2 l) => l.H();

    [MethodImpl(MethodImplOptions.NoInlining)] static int NF(IN n) => n.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int N2F(IN2 n) => n.F();

    [MethodImpl(MethodImplOptions.NoInlining)] static int DynF(IDyn d) => d.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int DynCastF(object o) => ((IDyn)o).F();

    [MethodImpl(MethodImplOptions.NoInlining)] static int CcF(ICc c) => c.F();

    // The call is inlined into a method that knows more about the receiver.
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static int VFInl(VBase v) => v.F();
    [MethodImpl(MethodImplOptions.NoInlining)] static int VFLate(int k) => VFInl(new VOver { K = k }) + VFInl(new V1 { K = k });
    [MethodImpl(MethodImplOptions.NoInlining)] static int VFLateOpaque(VBase a, VBase b) => VFInl(a) * 3 + VFInl(b);

    delegate int NullCall();

    // A zkVM guest has no signals: a load through null is a fault of the guest,
    // not a NullReferenceException, with or without devirtualization. So the
    // null receivers run only when the program is built with -d NULL_RECEIVERS,
    // for a target that turns the fault into the exception. Without it the null
    // check of a devirtualized call can only be read in the disassembly (a
    // `ld zero, 0(a0)` in front of the direct call or the inlined body).
    static void ExpectNull(string name, NullCall call)
    {
#if NULL_RECEIVERS
        int got;
        try { got = call(); Console.WriteLine("FAIL " + name + " expected NullReferenceException got value " + got); fails++; }
        catch (NullReferenceException) { }
        catch (Exception e) { Console.WriteLine("FAIL " + name + " expected NullReferenceException got " + e.GetType().Name); fails++; }
#endif
    }

    static void CaseSpec()
    {
        SpecBase[] objs = { new SpecBase(), new Spec1(), new Spec2(), new Spec3(), new Spec4(), new Spec5(), new Spec6() };
        int loop = 0;
        ISpec[] asSpec = new ISpec[objs.Length];
        for (int i = 0; i < objs.Length; i++)
        {
            int k = i * 5 + 2;
            objs[i].K = k;
            asSpec[i] = objs[i];
            Check("spec id " + i, k * 3 + 1, SpecId(objs[i]));
            Check("spec scale " + i, 1000003L * k + 7, SpecScale(objs[i], 1000003));
            Check("spec on " + i, k & 1, SpecOn(objs[i]) ? 1 : 0);
            Check("spec const " + i, 42, SpecConst(objs[i]));
            Check("spec const base " + i, 42, SpecConstBase(objs[i]));
            Check("spec cast " + i, k * 3 + 1, SpecAfterCast(objs[i]));
            Check("spec is " + i, k * 3 + 1, SpecAfterIs(objs[i]));
            loop = loop * 31 + (k * 3 + 1) + 42;
        }
        Check("spec loop", loop, SpecLoop(asSpec));
        Check("spec is other", -1, SpecAfterIs("text"));
        try { SpecAfterCast("text"); Check("spec cast other", 1, 0); } catch (InvalidCastException) { }

        ISpec none = Opaque<ISpec>(null);
        SpecBase noneBase = Opaque<SpecBase>(null);
        ExpectNull("spec null id", () => SpecId(none));
        ExpectNull("spec null scale", () => (int)SpecScale(none, 3));
        ExpectNull("spec null on", () => SpecOn(none) ? 1 : 0);
        ExpectNull("spec null const", () => SpecConst(none));
        ExpectNull("spec null const base", () => SpecConstBase(noneBase));
        ExpectNull("spec null cast", () => SpecAfterCast(null));
        ISpec[] withNull = { objs[0], null, objs[1] };
        ExpectNull("spec null in loop", () => SpecLoop(withNull));
    }

    static void CaseReimplemented()
    {
        ReBase[] objs = { new ReBase(), new Re1(), new Re2(), new Re3(), new Re4(), new Re5(), new ReAgain() };
        for (int i = 0; i < objs.Length; i++)
        {
            int e = i == 6 ? 20 : 10;
            Check("reimpl " + i, e, ReF(objs[i]));
            Check("reimpl via base " + i, e, ReBaseF(objs[i]));
        }
        ExpectNull("reimpl null", () => ReF(Opaque<IRe>(null)));
    }

    static void CaseVirtual()
    {
        VBase[] objs = { new VBase(), new V1(), new V2(), new V3(), new V4(), new V5(), new VOver() };
        for (int i = 0; i < objs.Length; i++)
        {
            objs[i].K = i + 1;
            Check("virtual F " + i, i + 1 + (i == 6 ? 1000 : 100), VF(objs[i]));
            Check("virtual G " + i, i + 1 + 200, VG(objs[i]));
        }
        ExpectNull("virtual null F", () => VF(Opaque<VBase>(null)));
        ExpectNull("virtual null G", () => VG(Opaque<VBase>(null)));
        Check("virtual late", 7 + 1000 + 7 + 100, VFLate(7));
        Check("virtual late opaque", (7 + 1000) * 3 + 2 + 100, VFLateOpaque(objs[6], objs[1]));
    }

    static void CaseActivated()
    {
        ActBase[] plain = { new ActBase(), new Act1(), new Act2(), new Act3(), new Act4(), new Act5() };
        for (int i = 0; i < plain.Length; i++)
        {
            Check("activated plain " + i, 1, ActF(plain[i]));
            Check("activated plain base " + i, 1, ActBaseF(plain[i]));
        }
        ActBase g = Activator.CreateInstance<ActGeneric>();
        Check("activated generic", 2, ActF(g));
        Check("activated generic base", 2, ActBaseF(g));
        ActBase n = Factory<ActNewT>.Make();
        Check("activated new T", 3, ActF(n));
        Check("activated new T base", 3, ActBaseF(n));
        ActBase t = (ActBase)Activator.CreateInstance(typeof(ActType));
        Check("activated type", 4, ActF(t));
        Check("activated type base", 4, ActBaseF(t));
        ActBase u = (ActBase)RuntimeHelpers.GetUninitializedObject(typeof(ActUninit));
        Check("activated uninit", 5, ActF(u));
        Check("activated uninit base", 5, ActBaseF(u));
        ActBase c = (ActBase)((ActClone)RuntimeHelpers.GetUninitializedObject(typeof(ActClone))).Clone();
        Check("activated clone", 6, ActF(c));
        Check("activated clone base", 6, ActBaseF(c));
    }

    static void CaseArrays()
    {
        var list = new ElemList();
        Elem[] arr = { new Elem { V = 11 }, new Elem { V = 22 }, new Elem { V = 33 } };
        Check("elem list at", -2, ElemAt(list, 1));
        Check("elem array at", 22, ElemAt(arr, 1));
        Check("elem list count", 77, ElemCount(list));
        Check("elem array count", 3, ElemCount(arr));
        Check("elem list at (IList)", -3, ElemAtL(list, 2));
        Check("elem array at (IList)", 33, ElemAtL(arr, 2));
        Check("elem list count (ICollection)", 77, ElemCountC(list));
        Check("elem array count (ICollection)", 3, ElemCountC(arr));
        Check("elem list index of", -5, ElemIndexOf(list, arr[2]));
        Check("elem array index of", 2, ElemIndexOf(arr, arr[2]));

        var items = new ItemList();
        Item[] itemArr = { new Item { V = 1 }, new Item { V = 2 } };
        SubItem[] subArr = { new SubItem { V = 3 }, new SubItem { V = 4 }, new SubItem { V = 5 } };
        var subs = new SubItemList();
        Check("item list at", -9, ItemAt(items, 1));
        Check("item array at", 2, ItemAt(itemArr, 1));
        Check("subitem array at", 4, ItemAt(subArr, 1));
        Check("subitem list at", -19, ItemAt(subs, 1));
        Check("item list count", 55, ItemCount(items));
        Check("item array count", 2, ItemCount(itemArr));
        Check("subitem array count", 3, ItemCount(subArr));
        Check("subitem list count", 66, ItemCount(subs));
        Check("item list count (collection)", 55, ItemCountC(items));
        Check("subitem array count (collection)", 3, ItemCountC(subArr));
        Check("subitem list count (collection)", 66, ItemCountC(subs));
    }

    static void CaseVariance()
    {
        IProducer<Animal>[] ps = { new AnimalProducer(), new AnimalProducer2(), new AnimalProducer3(), new AnimalProducer4(), new AnimalProducer5() };
        for (int i = 0; i < ps.Length; i++)
        {
            Check("producer tag " + i, 1, ProdTag(ps[i]));
            Check("producer legs " + i, 0, ProdLegs(ps[i]));
        }
        IProducer<Animal> dogs = new DogProducer();
        Check("producer tag variant", 2, ProdTag(dogs));
        Check("producer legs variant", 4, ProdLegs(dogs));

        Dog d = new Dog();
        IConsumer<Dog>[] cs = { new DogConsumer(), new DogConsumer2(), new DogConsumer3(), new DogConsumer4(), new DogConsumer5() };
        for (int i = 0; i < cs.Length; i++) Check("consumer " + i, 14, ConsEat(cs[i], d));
        IConsumer<Dog> any = new AnimalConsumer();
        Check("consumer variant", 24, ConsEat(any, d));
    }

    static void CaseDefaultMethods()
    {
        IDim[] ds = { new Dim1(), new Dim2(), new Dim3(), new Dim4(), new Dim5(), new Dim6() };
        int[] g = { 1, 2, 3, 4, 5, 5 };
        for (int i = 0; i < ds.Length; i++) Check("dim " + i, 100 + g[i], DimF(ds[i]));
        ExpectNull("dim null", () => DimF(Opaque<IDim>(null)));

        IDimO[] os = { new DimO1(), new DimO2(), new DimO3(), new DimO4(), new DimO5(), new DimOver(), new DimViaDerived() };
        int[] e = { 101, 102, 103, 104, 105, 600, 707 };
        for (int i = 0; i < os.Length; i++) Check("dim replaced " + i, e[i], DimOF(os[i]));
    }

    static void CaseGenerics()
    {
        GBase<int>[] gi = { new GBase<int>(), new GI1(), new GI2(), new GI3(), new GI4(), new GI5() };
        for (int i = 0; i < gi.Length; i++)
        {
            gi[i].V = 1000 + i;
            Check("generic int " + i, 1000 + i, GInt(gi[i]));
            Check("generic int size " + i, 4, GIntSize(gi[i]));
            Check("generic int shared " + i, 1000 + i, GShared(gi[i]));
            Check("generic int base " + i, 1000 + i, GBaseShared(gi[i]));
        }
        GBase<string>[] gs = { new GBase<string>(), new GS1(), new GS2(), new GS3(), new GS4(), new GS5() };
        string[] text = { "a", "bb", "ccc", "dddd", "eeeee", "ffffff" };
        for (int i = 0; i < gs.Length; i++)
        {
            gs[i].V = text[i];
            Check("generic string " + i, i + 1, GStr(gs[i]).Length);
            Check("generic string size " + i, IntPtr.Size, GStrSize(gs[i]));
            Check("generic string shared " + i, i + 1, GShared(gs[i]).Length);
            Check("generic string shared size " + i, IntPtr.Size, GSharedSize(gs[i]));
            Check("generic string base " + i, i + 1, GBaseShared(gs[i]).Length);
        }
        var go = new GO1 { V = text };
        Check("generic object shared", 6, ((string[])GShared<object>(go)).Length);
        Check("generic object shared size", IntPtr.Size, GSharedSize<object>(go));
        GBase<long>[] gl = { new GL1<int> { V = 1L << 40 }, new GL1<string> { V = 2L << 40 }, new GL1<object> { V = 3L << 40 }, new GL1<byte> { V = 4L << 40 }, new GBase<long> { V = 5L << 40 } };
        for (int i = 0; i < gl.Length; i++) Check("generic long " + i, (long)(i + 1) << 40, GLong(gl[i]));
        ExpectNull("generic null int", () => GInt(Opaque<IG<int>>(null)));
        ExpectNull("generic null string size", () => GStrSize(Opaque<IG<string>>(null)));
        ExpectNull("generic null shared size", () => GSharedSize(Opaque<IG<string>>(null)));
    }

    static void CaseAbstract()
    {
        HRoot[] hs = { new HMid(), new H1(), new H2(), new H3(), new H4(), new H5() };
        for (int i = 0; i < hs.Length; i++)
        {
            hs[i].K = i * 7;
            Check("abstract F " + i, i * 7 + 5, HF(hs[i]));
            Check("abstract twice " + i, (i * 7 + 5) * 2, HTwice(hs[i]));
        }
        ExpectNull("abstract null", () => HF(Opaque<HRoot>(null)));
        HRoot2[] h2 = { new HMid2(), new H21(), new H22(), new H23(), new H24(), new H2Leaf() };
        for (int i = 0; i < h2.Length; i++)
        {
            h2[i].K = i * 7;
            Check("abstract with leaf " + i, i * 7 + (i == 5 ? 50 : 5), H2F(h2[i]));
            Check("abstract with leaf mid " + i, i * 7 + (i == 5 ? 50 : 5), H2MidF((HMid2)h2[i]));
        }
    }

    static void CaseStruct()
    {
        IMix[] ms = { new MixBase(), new Mix1(), new Mix2(), new Mix3(), new Mix4() };
        for (int i = 0; i < ms.Length; i++)
        {
            Check("mix class " + i, 1, MixF(ms[i]));
            Check("mix class generic " + i, 1, MixGeneric(ms[i]));
        }
        Check("mix struct boxed", 1005, MixF(new MixStruct { V = 5 }));
        Check("mix struct generic", 1006, MixGeneric(new MixStruct { V = 6 }));
        Check("mix struct boxed generic", 1007, MixGeneric<IMix>(new MixStruct { V = 7 }));
    }

    static void CaseInner()
    {
        IK[] ks = { new KBase(), new K1(), new K2(), new K3(), new K4(), new K5() };
        int[] e = { 10, 20, 30, 10, 50, 50 };
        for (int i = 0; i < ks.Length; i++) Check("inner virtual " + i, e[i], KF(ks[i]));

        LBase[] ls = { new LBase(), new L1(), new L2(), new L3(), new L4(), new L5() };
        for (int i = 0; i < ls.Length; i++)
        {
            ls[i].K = i * 3;
            Check("inherited interface IL1.F " + i, i * 3 + 1, L1F(ls[i]));
            Check("inherited interface IL2.F " + i, i * 3 + 1, L2F(ls[i]));
            Check("inherited interface IL2.H " + i, i * 3 + 2, L2H(ls[i]));
        }
        Check("inherited interface other", -1, L1F(new LOnly1()));

        IN[] ns = { new NBase(), new N1(), new N2(), new N3(), new N4(), new N5() };
        for (int i = 0; i < ns.Length; i++) Check("hidden " + i, 1, NF(ns[i]));
        IN2[] n2 = { new NBase2(), new N21(), new N22(), new N23(), new N24(), new N2Again() };
        for (int i = 0; i < n2.Length; i++) Check("hidden and implemented again " + i, i == 5 ? 3 : 1, N2F(n2[i]));
    }

    static void CaseDynamic()
    {
        IDyn[] ds = { new DynBase(), new Dyn1(), new Dyn2(), new Dyn3(), new Dyn4() };
        for (int i = 0; i < ds.Length; i++) Check("dynamic plain " + i, 1, DynF(ds[i]));
        object castable = Opaque<object>(new DynCastable());
        try
        {
            Check("dynamic castable via cast", 77, DynCastF(castable));
            Check("dynamic castable", 77, DynF((IDyn)castable));
        }
        catch (Exception ex)
        {
            // The runtime may not support such objects at all; that is not the compiler's fault.
            Console.WriteLine("NOTE dynamic castable not supported here: " + ex.GetType().Name);
        }
    }

    static void CaseStatics()
    {
        ICc[] cs = { new Cc1(), new Cc2(), new Cc3(), new Cc4(), new Cc5(), new CcBase() };
        for (int i = 0; i < cs.Length; i++) Check("static state " + i, 31, CcF(cs[i]));
        Check("static state inits", 1, CcBase.Inits);
    }

    static int Main()
    {
        CaseSpec();
        CaseReimplemented();
        CaseVirtual();
        CaseActivated();
        CaseArrays();
        CaseVariance();
        CaseDefaultMethods();
        CaseGenerics();
        CaseAbstract();
        CaseStruct();
        CaseInner();
        CaseDynamic();
        CaseStatics();

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
