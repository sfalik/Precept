# Canon-against-canon check 3: identifiers canon uses as though they were declared,
# that no canonical document declares.
#
# Run from the repository root:
#   python3 docs/working-reset/phase-2/checks/undeclared-identifiers.py
#
# Builds the set of everything canon declares - type names, enum members, record members,
# method names - from every fenced code block, then reports PascalCase identifiers used in
# a declaration-shaped position (a variant after `new`, a member after a dot, a base type
# after a colon) that are in none of them and are not .NET framework types.
#
# What it cannot do. The BCL stoplist is finite and hand-maintained; an unfamiliar framework
# type will appear as a candidate. Read the list, do not trust it.
import re, pathlib, collections

roots = ["docs/language","docs/compiler","docs/runtime","docs/tooling"]
files = [p for r in roots for p in pathlib.Path(r).rglob("*.md")]
files += [pathlib.Path("docs/compiler-and-runtime-design.md"), pathlib.Path("README.md")]
files = sorted({f for f in files if f.exists() and "archive" not in str(f)})

BCL = set("""String Int32 Int64 Boolean Object Type Task ValueTask Span ReadOnlySpan Memory
Array List Dictionary HashSet SortedDictionary IReadOnlyList IReadOnlyDictionary IEnumerable
IList ICollection ImmutableArray ImmutableList ImmutableDictionary ImmutableHashSet
FrozenDictionary FrozenSet ArrayPool JsonElement JsonSerializer JsonDocument Nullable
Exception InvalidOperationException ArgumentException NotImplementedException Func Action
Guid DateTime DateTimeOffset TimeSpan Decimal Double Single Byte Char Math Convert Enum
Attribute AttributeUsage AttributeTargets Debug Trace StringBuilder Encoding Regex Match
Comparer EqualityComparer StringComparison CultureInfo Console File Path Directory Stream
Instant LocalDate LocalTime LocalDateTime ZonedDateTime Period Duration Offset DateTimeZone
IsoDayOfWeek CalendarSystem Interval Range Index Tuple KeyValuePair Lazy Version Uri
CancellationToken IDisposable IEquatable IComparable ISpanFormattable IFormatProvider
Assembly MethodInfo PropertyInfo FieldInfo BindingFlags Activator Expression Queryable
MethodImpl MethodImplOptions AggressiveInlining Parallel Interlocked Volatile Unsafe MemoryMarshal Vector Buffer GC WeakReference
Roslyn DiagnosticAnalyzer SyntaxNode SemanticModel CodeFixProvider Xunit Fact Theory
InlineData FluentAssertions Should NotNull Required Obsolete Serializable Flags""".split())

declre = re.compile(r'^\s*(?:public\s+|internal\s+|private\s+|protected\s+|sealed\s+|abstract\s+'
                    r'|static\s+|partial\s+|readonly\s+|ref\s+|new\s+|virtual\s+|override\s+)*'
                    r'(?:record struct|record|class|struct|enum|interface|delegate)\s+([A-Z][A-Za-z0-9_]*)')
memre  = re.compile(r'\b([A-Z][A-Za-z0-9_]*)\s*(?:\{\s*get|\(|;|,|=)')

declared, used = set(), collections.defaultdict(list)
blocks = []
for f in files:
    lines = f.read_text().splitlines()
    infence = False
    for i, l in enumerate(lines, 1):
        if l.lstrip().startswith("```"):
            # Only C# fences. A `precept` fence is DSL source, where PascalCase names are
            # an author's field and event names and mean nothing about canon's own types.
            tag = l.strip().strip("`").strip().lower()
            infence = (not infence) and tag in ("csharp", "c#", "cs")
            continue
        if not infence: continue
        blocks.append((str(f), i, l))
        m = declre.match(l)
        if m: declared.add(m.group(1))

# every identifier in a declaring position anywhere counts as declared
for f, i, l in blocks:
    code = re.sub(r'//.*', '', l)
    m = declre.match(code)
    if m: declared.add(m.group(1))
    # enum members and record members: bare PascalCase followed by = , ; ) or { get
    for mm in memre.finditer(code): declared.add(mm.group(1))
    # primary-constructor parameter names
    for mm in re.finditer(r'\b[A-Za-z_][\w<>,\[\]\?\.]*\s+([A-Z][A-Za-z0-9_]*)\s*[,)]', code):
        declared.add(mm.group(1))

# usages in a shape that implies the thing exists
USE = [re.compile(r'\bnew\s+([A-Z][A-Za-z0-9_]*)\s*[\(\{]'),
       re.compile(r'\breturn\s+new\s+[A-Za-z0-9_]*\.([A-Z][A-Za-z0-9_]*)\s*\('),
       re.compile(r'\b[A-Z][A-Za-z0-9_]*\.([A-Z][A-Za-z0-9_]*)\b'),
       re.compile(r':\s*([A-Z][A-Za-z0-9_]*)\s*[,;\(]')]
for f, i, l in blocks:
    code = re.sub(r'//.*', '', l)
    if declre.match(code): continue
    for pat in USE:
        for mm in pat.finditer(code):
            nm = mm.group(1)
            if nm in declared or nm in BCL: continue
            if len(nm) < 3: continue
            used[nm].append((f, i, l.strip()[:130]))

print("Identifiers used in a declaration-shaped position that nothing declares")
print("=" * 72)
multi = {k: v for k, v in used.items() if len(v) >= 2}
for k in sorted(multi, key=lambda x: -len(multi[x])):
    fs = sorted({f for f, _, _ in multi[k]})
    print(f"\n  {k} — {len(multi[k])} uses across {len(fs)} document(s)")
    for f, i, txt in multi[k][:6]:
        print(f"     {f}:{i}")
        print(f"        | {txt}")
print(f"\n  {len(multi)} identifiers used two or more times and never declared.")
print(f"  ({len(used) - len(multi)} more used exactly once - not printed.)")
