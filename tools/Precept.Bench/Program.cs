using BenchmarkDotNet.Running;
using Precept.Bench;

// Number-model benchmark: regular `decimal` vs arbitrary-precision arithmetic,
// to inform the fixed-decimal vs arbitrary-precision decision (readiness plan Phase 0).
//
//  * decimal          — what Precept uses today (128-bit, ~28-29 digits, ~7.9e28 range).
//  * UcumExactFactor  — the EXISTING arbitrary-precision rational in src/ (BigInteger num/den +
//                       base-10 exponent, GCD-normalized each op). Multiply/Divide/Pow only — NO add.
//  * BigRational      — a minimal PROTOTYPE value type (same shape, WITH Add/Sub) so the value-domain
//                       arithmetic the runtime would do (incl. add + a realistic chain) can be measured.
//                       Bench-only; not production.

// First: the size-growth demo (the rational blow-up concern) — cheap, printed before the timed run.
BlowUp.Print();

// Then: the rigorous timing via BenchmarkDotNet.
BenchmarkRunner.Run<ArithmeticBench>();
