namespace Precept.Language;

public sealed record UcumPrefix(
    string Code,
    string Name,
    UcumExactFactor Factor,
    int Order);
