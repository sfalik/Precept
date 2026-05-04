namespace Precept.Runtime;

/// <summary>
/// Fluent builder for event arguments — typed lane ingress for
/// <see cref="Runtime.Precept.Create(Action{IArgBuilder}?)"/> and
/// <see cref="Version.Fire(string, Action{IArgBuilder}?)"/>.
/// </summary>
/// <remarks>
/// Each <see cref="Set{T}"/> call is resolved through the registered
/// <c>TypeRuntime&lt;T&gt;</c> for zero-boxing conversion to <see cref="PreceptValue"/>.
/// The builder internally produces a slot array populated via a presence mask.
/// Unset args remain absent; <see cref="InvalidArgs"/> is returned if required args
/// are missing at evaluation time.
/// </remarks>
public interface IArgBuilder
{
    IArgBuilder Set<T>(string name, T value);
}
