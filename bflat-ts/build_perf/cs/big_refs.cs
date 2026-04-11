// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Build-performance baseline: a single C# file that exercises a broad
// cross-section of the .NET BCL to stress the bflat AOT compiler –
// many generic instantiations, many assembly references, complex LINQ
// pipelines, and diverse type usage across 20+ assemblies.
//
// Compilation flags assumed:
//   --no-globalization --no-stacktrace-data --no-pthread --no-pie
//   --stdlib dotnet
//
// run=FALSE – the binary is compiled but never executed; build time
// (wall-clock seconds for the bflat invocation) is the only metric.

// ── System.Collections.* ─────────────────────────────────────────────
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.Specialized;
// ── System.Linq ──────────────────────────────────────────────────────
using System.Linq;
// ── System.Text.* ────────────────────────────────────────────────────
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
// ── System.Numerics ──────────────────────────────────────────────────
using System.Numerics;
// ── System.Security.Cryptography ─────────────────────────────────────
using System.Security.Cryptography;
// ── System.IO.* ──────────────────────────────────────────────────────
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
// ── System.Buffers ───────────────────────────────────────────────────
using System.Buffers;
// ── System.Reflection ────────────────────────────────────────────────
using System.Reflection;
// ── System.Runtime.* ─────────────────────────────────────────────────
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
// ── System.Threading.* ───────────────────────────────────────────────
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
// ── System.Diagnostics ───────────────────────────────────────────────
using System.Diagnostics;
// ── System.Formats ───────────────────────────────────────────────────
using System.Formats.Asn1;
// ── Base ─────────────────────────────────────────────────────────────
using System;
using System.Diagnostics.CodeAnalysis;

// =============================================================================
// Generic value types
// =============================================================================

/// <summary>Discriminated union: Ok(T) or Err(string).</summary>
readonly struct Result<T>
{
    public bool   IsOk  { get; }
    public T      Value { get; }
    public string Error { get; }

    private Result(bool ok, T v, string e) { IsOk = ok; Value = v; Error = e; }

    public static Result<T> Ok(T v)        => new(true,  v,       null);
    public static Result<T> Err(string e)  => new(false, default, e);

    public Result<U> Map<U>(Func<T, U> f) =>
        IsOk ? Result<U>.Ok(f(Value)) : Result<U>.Err(Error);

    public T GetOrDefault(T fallback) => IsOk ? Value : fallback;
    public override string ToString()  => IsOk ? $"Ok({Value})" : $"Err({Error})";
}

/// <summary>Typed pair.</summary>
readonly record struct Pair<A, B>(A Fst, B Snd);

/// <summary>Typed triple.</summary>
readonly record struct Triple<A, B, C>(A Fst, B Snd, C Thd);

// =============================================================================
// Custom comparers  (exercises IComparer<T> / IEqualityComparer<T>)
// =============================================================================

sealed class ProjectionComparer<T, K> : IComparer<T> where K : IComparable<K>
{
    readonly Func<T, K> _key;
    public ProjectionComparer(Func<T, K> key) => _key = key;
    public int Compare(T x, T y) => _key(x).CompareTo(_key(y));
}

sealed class LambdaEq<T> : IEqualityComparer<T>
{
    readonly Func<T, T, bool> _eq;
    readonly Func<T, int>     _hash;
    public LambdaEq(Func<T, T, bool> eq, Func<T, int> hash) { _eq = eq; _hash = hash; }
    public bool Equals(T x, T y)  => _eq(x, y);
    public int  GetHashCode(T obj) => _hash(obj);
}

// =============================================================================
// Math utilities  (BigInteger, bit ops)
// =============================================================================

static class MathUtils
{
    /// <summary>Sieve of Eratosthenes.</summary>
    public static ImmutableArray<int> Primes(int upTo)
    {
        var sieve = new bool[upTo + 1];
        var builder = ImmutableArray.CreateBuilder<int>();
        for (int i = 2; i <= upTo; i++)
        {
            if (sieve[i]) continue;
            builder.Add(i);
            for (int j = i + i; j <= upTo; j += i) sieve[j] = true;
        }
        return builder.ToImmutable();
    }

    /// <summary>Factorial using BigInteger.</summary>
    public static BigInteger Factorial(int n)
    {
        BigInteger r = BigInteger.One;
        for (int i = 2; i <= n; i++) r *= i;
        return r;
    }

    /// <summary>GCD via Euclidean algorithm.</summary>
    public static BigInteger Gcd(BigInteger a, BigInteger b)
    {
        while (b != BigInteger.Zero) { var t = b; b = a % b; a = t; }
        return a;
    }

    /// <summary>Ceiling log₂.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilLog2(int n)
    {
        if (n <= 1) return 0;
        int bits = 0; n--;
        while (n > 0) { n >>= 1; bits++; }
        return bits;
    }

    /// <summary>Popcount via bit-twiddling (no intrinsics).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Popcount(uint v)
    {
        v -= (v >> 1) & 0x55555555u;
        v  = (v & 0x33333333u) + ((v >> 2) & 0x33333333u);
        return (int)(((v + (v >> 4) & 0x0F0F0F0Fu) * 0x01010101u) >> 24);
    }
}

// =============================================================================
// Text utilities  (Regex, StringBuilder, Encoding, Base64)
// =============================================================================

static class TextUtils
{
    static readonly Regex _ws = new(@"\s+", RegexOptions.Compiled);

    public static string Normalise(string s) =>
        _ws.Replace(s.Trim(), " ");

    public static Dictionary<string, int> Frequency(string text)
    {
        var freq = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var tok in _ws.Split(text))
        {
            if (tok.Length == 0) continue;
            freq.TryGetValue(tok, out int c);
            freq[tok] = c + 1;
        }
        return freq;
    }

    public static string ToBase64(ReadOnlySpan<byte> data)
    {
        var buf = new char[((data.Length + 2) / 3) * 4];
        Convert.TryToBase64Chars(data, buf, out int written);
        return new string(buf, 0, written);
    }

    public static byte[] FromBase64(string s) => Convert.FromBase64String(s);

    public static string BuildSv(IEnumerable<IEnumerable<string>> rows, char sep = '\t')
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            sb.AppendJoin(sep, row);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

// =============================================================================
// Collection utilities  (Immutable, Concurrent, chunking, merging)
// =============================================================================

static class CollUtils
{
    public static ImmutableSortedDictionary<K, IReadOnlyList<V>>
        GroupSorted<T, K, V>(IEnumerable<T> src, Func<T, K> key, Func<T, V> val)
        where K : notnull
    {
        return src
            .GroupBy(key)
            .ToImmutableSortedDictionary(
                g => g.Key,
                g => (IReadOnlyList<V>)g.Select(val).ToList());
    }

    public static ConcurrentDictionary<K, V> Concurrent<K, V>(
        IEnumerable<(K, V)> pairs) where K : notnull
    {
        var d = new ConcurrentDictionary<K, V>();
        foreach (var (k, v) in pairs) d[k] = v;
        return d;
    }

    public static IEnumerable<IReadOnlyList<T>> Chunk<T>(IEnumerable<T> src, int n)
    {
        var buf = new List<T>(n);
        foreach (var x in src)
        {
            buf.Add(x);
            if (buf.Count == n) { yield return buf.AsReadOnly(); buf = new(n); }
        }
        if (buf.Count > 0) yield return buf.AsReadOnly();
    }

    public static IEnumerable<T> Interleave<T>(IEnumerable<T> a, IEnumerable<T> b)
    {
        using var ea = a.GetEnumerator();
        using var eb = b.GetEnumerator();
        bool ha = ea.MoveNext(), hb = eb.MoveNext();
        while (ha || hb)
        {
            if (ha) { yield return ea.Current; ha = ea.MoveNext(); }
            if (hb) { yield return eb.Current; hb = eb.MoveNext(); }
        }
    }
}

// =============================================================================
// In-memory I/O  (MemoryStream, BinaryWriter/Reader, DeflateStream)
// =============================================================================

static class IoUtils
{
    public static byte[] ToBytes<T>(T v, Action<BinaryWriter, T> write)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        write(bw, v);
        return ms.ToArray();
    }

    public static T FromBytes<T>(byte[] data, Func<BinaryReader, T> read)
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms, Encoding.UTF8);
        return read(br);
    }

    public static byte[] Deflate(byte[] src)
    {
        using var dst = new MemoryStream();
        using (var ds = new DeflateStream(dst, CompressionLevel.Fastest))
            ds.Write(src, 0, src.Length);
        return dst.ToArray();
    }

    public static byte[] Inflate(byte[] src)
    {
        using var dst = new MemoryStream();
        using var ds  = new DeflateStream(new MemoryStream(src), CompressionMode.Decompress);
        ds.CopyTo(dst);
        return dst.ToArray();
    }
}

// =============================================================================
// Cryptography  (SHA-256, HMAC-SHA-256, AES, RNG)
// =============================================================================

static class CryptoUtils
{
    public static byte[] Sha256(ReadOnlySpan<byte> data) => SHA256.HashData(data);

    public static byte[] HmacSha256(byte[] key, ReadOnlySpan<byte> data) =>
        HMACSHA256.HashData(key, data);

    public static byte[] Sha512(ReadOnlySpan<byte> data) => SHA512.HashData(data);

    public static byte[] RandomBytes(int n)
    {
        var buf = new byte[n];
        RandomNumberGenerator.Fill(buf);
        return buf;
    }

    public static (byte[] ct, byte[] iv, byte[] key) AesEncrypt(byte[] pt)
    {
        using var aes = Aes.Create();
        aes.GenerateKey(); aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        using var ms  = new MemoryStream();
        using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write, leaveOpen: true))
            cs.Write(pt);
        return (ms.ToArray(), (byte[])aes.IV.Clone(), (byte[])aes.Key.Clone());
    }

    public static byte[] AesDecrypt(byte[] ct, byte[] iv, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key; aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        using var ms  = new MemoryStream();
        using (var cs = new CryptoStream(new MemoryStream(ct), dec, CryptoStreamMode.Read))
            cs.CopyTo(ms);
        return ms.ToArray();
    }
}

// =============================================================================
// JSON  (System.Text.Json – source-gen context for AOT)
// =============================================================================

record MetricPoint(string Name, long ValueMs, string Tag);
record PerfReport(string Compiler, string Arch, string Libc, MetricPoint[] Points);

static class JsonUtils
{
    static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = true,
        Encoder              = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Serialize(PerfReport r) =>
        JsonSerializer.Serialize(r, _opts);

    public static JsonObject ToJsonObject(IDictionary<string, string> map)
    {
        var obj = new JsonObject();
        foreach (var (k, v) in map) obj[k] = JsonValue.Create(v);
        return obj;
    }
}

// =============================================================================
// LINQ pipelines  (exercises many LINQ operators)
// =============================================================================

record Student(string Name, int Grade, string[] Courses);

static class LinqPipelines
{
    /// <summary>Per-course average grade, sorted descending.</summary>
    public static List<(string Course, double Avg, int N)>
        CourseStats(IEnumerable<Student> students) =>
        students
            .SelectMany(s => s.Courses.Select(c => (c, s.Grade)))
            .GroupBy(x => x.c)
            .Select(g => (g.Key, g.Average(x => (double)x.Grade), g.Count()))
            .OrderByDescending(x => x.Item2)
            .ThenBy(x => x.Key)
            .ToList();

    /// <summary>Index words by first letter.</summary>
    public static ILookup<char, string> ByFirstLetter(IEnumerable<string> words) =>
        words.Where(w => w.Length > 0)
             .ToLookup(w => char.ToUpperInvariant(w[0]));

    /// <summary>Running sum using Aggregate.</summary>
    public static IReadOnlyList<long> RunningSum(IEnumerable<int> xs)
    {
        long acc = 0;
        return xs.Select(x => acc += x).ToList();
    }

    /// <summary>Zip two sequences into pairs.</summary>
    public static IEnumerable<Pair<A, B>> ZipPairs<A, B>(
        IEnumerable<A> a, IEnumerable<B> b) =>
        a.Zip(b, (x, y) => new Pair<A, B>(x, y));
}

// =============================================================================
// Async / Task  (Channel + ValueTask)
// =============================================================================

static class AsyncUtils
{
    public static async Task<Result<int>> TryParseIntAsync(string s)
    {
        await Task.CompletedTask;
        return int.TryParse(s, out int v)
            ? Result<int>.Ok(v)
            : Result<int>.Err($"not an int: '{s}'");
    }

    public static async IAsyncEnumerable<int> RangeAsync(
        int start, int count,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (int i = start; i < start + count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            yield return i;
        }
    }

    public static ValueTask<T> Done<T>(T v) => ValueTask.FromResult(v);
}

// =============================================================================
// Memory / Span  (ArrayPool, MemoryPool, unsafe reads)
// =============================================================================

/// <summary>Delegate for Span-based fill callbacks (Span is a ref struct,
/// cannot be used as a Func type argument).</summary>
delegate int SpanByteAction(Span<byte> span);

static class MemUtils
{
    public static int SumSpan(ReadOnlySpan<int> s)
    {
        int t = 0; foreach (int v in s) t += v; return t;
    }

    public static void Reverse<T>(Span<T> s)
    {
        int lo = 0, hi = s.Length - 1;
        while (lo < hi) { (s[lo], s[hi]) = (s[hi], s[lo]); lo++; hi--; }
    }

    public static byte[] RentProcess(int n, SpanByteAction fill)
    {
        byte[] arr = ArrayPool<byte>.Shared.Rent(n);
        try
        {
            int written = fill(arr.AsSpan(0, n));
            return arr[..written];
        }
        finally { ArrayPool<byte>.Shared.Return(arr); }
    }

    public static IMemoryOwner<T> RentOwned<T>(int minLen) =>
        MemoryPool<T>.Shared.Rent(minLen);
}

// =============================================================================
// DataContract attributes  (System.Runtime.Serialization)
// =============================================================================

[DataContract]
sealed class Measurement
{
    [DataMember(Order = 1)] public string Label   { get; set; } = "";
    [DataMember(Order = 2)] public long   ElapsedMs { get; set; }
    [DataMember(Order = 3)] public string Unit    { get; set; } = "ms";
}

// =============================================================================
// Reflection metadata helpers  (no dynamic invoke)
// =============================================================================

static class ReflUtils
{
    public static IReadOnlyList<string> PublicMethodNames<T>() =>
        typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance |
                             BindingFlags.Static | BindingFlags.DeclaredOnly)
                 .Select(m => m.Name)
                 .ToList();

    public static bool IsGenericType(Type t) => t.IsGenericType;
}

// =============================================================================
// Entry point
// =============================================================================

class Program
{
    static int Main()
    {
        // ── Primes & BigInteger ──────────────────────────────────────────────
        var primes = MathUtils.Primes(200);
        if (primes.Length < 46) return 1;                   // 46 primes ≤ 200

        BigInteger fact12 = MathUtils.Factorial(12);
        if (fact12 != 479001600) return 1;

        BigInteger gcd = MathUtils.Gcd(new BigInteger(48), new BigInteger(18));
        if (gcd != 6) return 1;

        if (MathUtils.CeilLog2(1024) != 10) return 1;
        if (MathUtils.Popcount(0b1011_1010u) != 5) return 1;

        // ── Text / Regex ─────────────────────────────────────────────────────
        var freq = TextUtils.Frequency("one two one three two one");
        if (!freq.ContainsKey("one") || freq["one"] != 3) return 1;
        if (TextUtils.Normalise("  hello   world  ") != "hello world") return 1;

        byte[] rawUtf8 = Encoding.UTF8.GetBytes("bflat-perf-test");
        string b64 = TextUtils.ToBase64(rawUtf8);
        if (TextUtils.FromBase64(b64).Length != rawUtf8.Length) return 1;

        // ── Collections & Immutable ──────────────────────────────────────────
        var grouped = CollUtils.GroupSorted(
            Enumerable.Range(0, 30).Select(i => (Key: $"k{i % 6}", Val: i)),
            x => x.Key, x => x.Val);
        if (grouped.Count != 6) return 1;

        var concurrent = CollUtils.Concurrent(
            Enumerable.Range(0, 10).Select(i => ($"key{i}", i * i)));
        if (concurrent.Count != 10) return 1;

        var chunks = CollUtils.Chunk(Enumerable.Range(1, 25), 4).ToList();
        if (chunks.Count != 7) return 1;

        var interleaved = CollUtils.Interleave(
            new[] { 1, 3, 5 }, new[] { 2, 4, 6 }).ToList();
        if (interleaved.Count != 6 || interleaved[1] != 3) return 1;

        // ── I/O (in-memory) ──────────────────────────────────────────────────
        byte[] serialized = IoUtils.ToBytes(42, (bw, v) => bw.Write(v));
        int deserialized  = IoUtils.FromBytes(serialized, br => br.ReadInt32());
        if (deserialized != 42) return 1;

        byte[] compressed   = IoUtils.Deflate(rawUtf8);
        byte[] decompressed = IoUtils.Inflate(compressed);
        if (decompressed.Length != rawUtf8.Length) return 1;

        // ── Cryptography ─────────────────────────────────────────────────────
        byte[] sha256 = CryptoUtils.Sha256(rawUtf8);
        if (sha256.Length != 32) return 1;

        byte[] sha512 = CryptoUtils.Sha512(rawUtf8);
        if (sha512.Length != 64) return 1;

        byte[] hmac = CryptoUtils.HmacSha256(new byte[32], rawUtf8);
        if (hmac.Length != 32) return 1;

        byte[] rnd = CryptoUtils.RandomBytes(64);
        if (rnd.Length != 64) return 1;

        var (ct, iv, key) = CryptoUtils.AesEncrypt(rawUtf8);
        byte[] pt = CryptoUtils.AesDecrypt(ct, iv, key);
        if (pt.Length != rawUtf8.Length) return 1;

        // ── JSON ─────────────────────────────────────────────────────────────
        var report = new PerfReport("bflat", "riscv64", "zisk_sim",
            new[] { new MetricPoint("compile_ms", 2500, "ms") });
        string json = JsonUtils.Serialize(report);
        if (!json.Contains("bflat")) return 1;

        var jobj = JsonUtils.ToJsonObject(
            new Dictionary<string, string> { ["arch"] = "riscv64", ["libc"] = "zisk_sim" });
        if (jobj["arch"]?.GetValue<string>() != "riscv64") return 1;

        // ── LINQ pipelines ───────────────────────────────────────────────────
        var students = new[]
        {
            new Student("Alice", 92, new[] { "Math", "CS", "Physics" }),
            new Student("Bob",   78, new[] { "CS",   "Art" }),
            new Student("Carol", 85, new[] { "Math", "Art", "Music"  }),
            new Student("Dave",  90, new[] { "CS",   "Math" }),
        };
        var stats = LinqPipelines.CourseStats(students);
        if (stats.Count == 0) return 1;

        var lookup = LinqPipelines.ByFirstLetter(
            new[] { "apple", "avocado", "banana", "cherry" });
        if (lookup['A'].Count() != 2) return 1;

        var running = LinqPipelines.RunningSum(new[] { 1, 2, 3, 4, 5 });
        if (running[4] != 15) return 1;

        var zipped = LinqPipelines.ZipPairs(
            new[] { "a", "b", "c" }, new[] { 1, 2, 3 }).ToList();
        if (zipped[2].Fst != "c" || zipped[2].Snd != 3) return 1;

        // ── Async (completes synchronously) ──────────────────────────────────
        var t1 = AsyncUtils.TryParseIntAsync("99");
        t1.Wait();
        if (!t1.Result.IsOk || t1.Result.Value != 99) return 1;

        var t2 = AsyncUtils.TryParseIntAsync("xyz");
        t2.Wait();
        if (t2.Result.IsOk) return 1;

        var doneTask = AsyncUtils.Done(42).AsTask();
        doneTask.Wait();
        if (doneTask.Result != 42) return 1;

        // ── Memory / Span / ArrayPool ─────────────────────────────────────────
        int[] arr = { 10, 20, 30, 40, 50 };
        if (MemUtils.SumSpan(arr) != 150) return 1;
        MemUtils.Reverse<int>(arr);
        if (arr[0] != 50 || arr[4] != 10) return 1;

        byte[] rented = MemUtils.RentProcess(8, span =>
        {
            for (int i = 0; i < span.Length; i++) span[i] = (byte)(i + 1);
            return span.Length;
        });
        if (rented.Length != 8 || rented[7] != 8) return 1;

        using (var owned = MemUtils.RentOwned<int>(16))
        {
            if (owned.Memory.Length < 16) return 1;
        }

        // ── Channels ─────────────────────────────────────────────────────────
        var ch = Channel.CreateBounded<string>(new BoundedChannelOptions(8)
            { FullMode = BoundedChannelFullMode.Wait });
        ch.Writer.TryWrite("hello");
        ch.Writer.TryWrite("world");
        ch.Writer.TryComplete();
        var msgs = new List<string>();
        while (ch.Reader.TryRead(out var msg)) msgs.Add(msg);
        if (msgs.Count != 2 || msgs[0] != "hello") return 1;

        // ── Pattern matching & switch expressions ─────────────────────────────
        object[] mixed = { 1, "hello", 3.14, true, (long)99 };
        int intSum = 0;
        foreach (var item in mixed)
        {
            intSum += item switch
            {
                int i    => i,
                long l   => (int)l,
                bool b   => b ? 1 : 0,
                string s => s.Length,
                _        => 0,
            };
        }
        if (intSum != 1 + 5 + 1 + 99) return 1;

        // ── Reflection (metadata only, no dynamic invoke) ─────────────────────
        if (!ReflUtils.IsGenericType(typeof(List<int>))) return 1;
        var methods = ReflUtils.PublicMethodNames<List<int>>();
        if (!methods.Contains("Add")) return 1;

        // ── DataContract attribute presence ──────────────────────────────────
        var m = new Measurement { Label = "compile", ElapsedMs = 2500 };
        if (m.Label != "compile") return 1;

        // ── Marshal / interop metadata ───────────────────────────────────────
        int szInt = Marshal.SizeOf<int>();
        if (szInt != 4) return 1;

        Console.WriteLine(
            $"build_perf/big_refs: ok  primes={primes.Length}  fact12={fact12}  " +
            $"sha256={sha256.Length}B  json={json.Length}ch  stats={stats.Count}  " +
            $"msgs={msgs.Count}  intSum={intSum}");
        return 0;
    }
}