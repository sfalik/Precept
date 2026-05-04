namespace Precept.Runtime;

/// <summary>
/// Fluent builder for field patches — typed lane ingress for
/// <see cref="Version.Update(Action{IFieldBuilder}?)"/> and
/// <see cref="Runtime.Precept.Create(Action{IArgBuilder}?)"/>.
/// </summary>
/// <remarks>
/// Each <see cref="Set{T}"/> call is resolved through the registered
/// <c>TypeRuntime&lt;T&gt;</c> for zero-boxing conversion to <see cref="PreceptValue"/>.
/// Access mode checks are applied after the patch is materialized.
/// </remarks>
public interface IFieldBuilder
{
    IFieldBuilder Set<T>(string name, T value);
}
