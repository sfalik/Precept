using System.Numerics;

namespace Precept.Bench;

// Minimal arbitrary-precision rational VALUE type — same internal shape as the production
// UcumExactFactor (BigInteger num/den, GCD-normalized each op) but with Add/Sub so the
// value-domain arithmetic the runtime would do can be measured. Exact for + - * /. Bench-only.
public readonly struct BigRational
{
    public readonly BigInteger Num;
    public readonly BigInteger Den;

    private BigRational(BigInteger num, BigInteger den, bool normalize)
    {
        if (normalize)
        {
            if (den.Sign < 0) { num = -num; den = -den; }
            var g = BigInteger.GreatestCommonDivisor(BigInteger.Abs(num), den);
            if (!g.IsZero && g != BigInteger.One) { num /= g; den /= g; }
        }
        Num = num;
        Den = den;
    }

    public static BigRational Zero => new(BigInteger.Zero, BigInteger.One, false);
    public static BigRational FromInt(int v) => new(v, BigInteger.One, false);

    // Exact decimal -> rational via the decimal's own mantissa/scale.
    public static BigRational Parse(decimal d)
    {
        int[] bits = decimal.GetBits(d);
        int scale = (bits[3] >> 16) & 0x7F;
        bool neg = (bits[3] & unchecked((int)0x80000000)) != 0;
        var mant = new BigInteger((uint)bits[0])
                 + (new BigInteger((uint)bits[1]) << 32)
                 + (new BigInteger((uint)bits[2]) << 64);
        if (neg) mant = -mant;
        return new BigRational(mant, BigInteger.Pow(10, scale), true);
    }

    public BigRational Add(BigRational o) => new(Num * o.Den + o.Num * Den, Den * o.Den, true);
    public BigRational Sub(BigRational o) => new(Num * o.Den - o.Num * Den, Den * o.Den, true);
    public BigRational Mul(BigRational o) => new(Num * o.Num, Den * o.Den, true);
    public BigRational Div(BigRational o) => new(Num * o.Den, Den * o.Num, true);

    public int NumDigits => Num.IsZero ? 1 : (int)(BigInteger.Abs(Num).GetBitLength() * 0.30103) + 1;
    public int DenDigits => (int)(BigInteger.Abs(Den).GetBitLength() * 0.30103) + 1;
}

// The rational blow-up demonstration: does the exact representation grow over a long chain?
public static class BlowUp
{
    public static void Print()
    {
        Console.WriteLine("\nRATIONAL SIZE GROWTH over a long accumulating chain (mul + add each step):\n");
        var acc = BigRational.Parse(100m);
        var step = BigRational.Parse(1.07m);
        var bump = BigRational.Parse(3.33m);
        int[] checkpoints = { 1, 10, 50, 100, 250, 500 };
        int ci = 0;
        for (int n = 1; n <= 500; n++)
        {
            acc = acc.Mul(step).Add(bump);
            if (ci < checkpoints.Length && n == checkpoints[ci])
            {
                Console.WriteLine($"  after {n,4} ops:  numerator {acc.NumDigits,5} digits, denominator {acc.DenDigits,5} digits");
                ci++;
            }
        }
        Console.WriteLine("  (decimal stays fixed-width 128-bit throughout; the exact rational grows unboundedly.)\n");
    }
}
