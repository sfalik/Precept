namespace Precept.Language;

public readonly record struct UcumToken(UcumTokenKind Kind, string Text, int Start, int Length);
