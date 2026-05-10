using System;
using System.Linq;
using Precept;
using Precept.LanguageServer;
using Precept.Pipeline;

var source = @"precept OrderItem
field Quantity as number
state Pending initial
event Activate
rule Quantity > 0 => \"ok\"";
var compilation = Compiler.Compile(source);
foreach (var symbol in OutlineSymbolProjector.Project(compilation))
{
    Console.WriteLine($"{symbol.Name}: range=({symbol.Range.StartLine},{symbol.Range.StartColumn})-({symbol.Range.EndLine},{symbol.Range.EndColumn}) sel=({symbol.SelectionRange.StartLine},{symbol.SelectionRange.StartColumn})-({symbol.SelectionRange.EndLine},{symbol.SelectionRange.EndColumn}) contained={Contains(symbol.Range, symbol.SelectionRange)}");
}

static bool Contains(SourceSpan outer, SourceSpan inner)
{
    var startsAfter = inner.StartLine > outer.StartLine || (inner.StartLine == outer.StartLine && inner.StartColumn >= outer.StartColumn);
    var endsBefore = inner.EndLine < outer.EndLine || (inner.EndLine == outer.EndLine && inner.EndColumn <= outer.EndColumn);
    return startsAfter && endsBefore;
}
