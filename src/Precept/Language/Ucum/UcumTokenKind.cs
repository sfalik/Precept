namespace Precept.Language;

public enum UcumTokenKind
{
    Atom = 1,
    Prefix = 2,
    Dot = 3,
    Slash = 4,
    OpenParen = 5,
    CloseParen = 6,
    Exponent = 7,
    Annotation = 8,
    EndOfInput = 9,
    Error = 10,
}
