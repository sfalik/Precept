var source = @"precept Sample
field Tags as set of string
state Draft initial";
var compilation = Precept.Compiler.Compile(source);
foreach (var c in compilation.ConstructManifest.Constructs) {
    var slot = c.GetSlot<Precept.Pipeline.TypeExpressionSlot>(Precept.Language.ConstructSlotKind.TypeExpression);
    Console.WriteLine($"Construct: {c.Meta.Kind}, typeSlot: {slot?.Span}");
}
foreach (var t in compilation.Tokens.Tokens.Where(t => t.Kind == Precept.Language.TokenKind.Set)) {
    Console.WriteLine($"Set token span: {t.Span}");
}
