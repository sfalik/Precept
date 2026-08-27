# Canon-against-canon check 4: every diagnostic code is attributed to a pipeline stage by
# the header it sits under. Does the stage's own document know the code exists?
#
# Run from the repository root:
#   python3 docs/working-reset/phase-2/checks/code-stage-attribution.py
#
# The DiagnosticCode enum in diagnostic-system.md is grouped by banner comments naming the
# stage that emits each block. Each stage has a canonical document. A code the emitting
# document has never heard of is either a code nothing emits or a stage document missing
# what it emits; either way two canonical documents disagree about the same fact.
import re, pathlib

SRC = pathlib.Path("docs/compiler/diagnostic-system.md")
# banner word -> the document that owns that stage
STAGE_DOC = {
    "lex":       ["docs/compiler/lexer.md"],
    "parse":     ["docs/compiler/parser.md"],
    "namebinder":["docs/compiler/name-binder.md"],
    "name":      ["docs/compiler/name-binder.md"],
    "type":      ["docs/compiler/type-checker.md"],
    "graph":     ["docs/compiler/graph-analyzer.md"],
    "proof":     ["docs/compiler/proof-engine.md"],
    "runtime":   ["docs/runtime/evaluator.md", "docs/runtime/runtime-api.md",
                  "docs/runtime/result-types.md", "docs/runtime/fault-system.md"],
    "tooling":   ["docs/compiler/tooling-surface.md", "docs/tooling/language-server.md",
                  "docs/tooling/mcp.md"],
}
lines = SRC.read_text().splitlines()
start = next(i for i, l in enumerate(lines) if l.strip() == "public enum DiagnosticCode")
stage, rows, unmapped = None, [], set()
for i in range(start, len(lines)):
    l = lines[i]
    if l.strip().startswith("```"): break
    b = re.match(r'\s*//\s*[─\-=]*\s*([A-Za-z][A-Za-z /()\-]*)', l)
    if b:
        word = b.group(1).strip().lower().split()[0].split("(")[0].strip(" /")
        # An unrecognised banner must not silently leave the previous stage in place —
        # that misattributes every code under it. UndeclaredArg was reported against the
        # proof engine for exactly this reason before `NameBinder` was added to the map.
        stage = word if word in STAGE_DOC else None
        if word not in STAGE_DOC: unmapped.add(word)
        continue
    m = re.match(r'\s*([A-Z][A-Za-z0-9_]*)\s*=\s*([0-9]+)', l)
    if m and stage: rows.append((m.group(1), int(m.group(2)), stage, i + 1))

text = {}
for docs in STAGE_DOC.values():
    for d in docs:
        p = pathlib.Path(d)
        text[d] = p.read_text() if p.exists() else ""

print("Diagnostic codes their own stage document has never heard of")
print("=" * 72)
missing = []
for name, val, stage, ln in rows:
    if not any(name in text[d] for d in STAGE_DOC[stage]):
        missing.append((stage, name, val, ln))
by = {}
for stage, name, val, ln in missing: by.setdefault(stage, []).append((val, name, ln))
for stage in sorted(by):
    docs = ", ".join(STAGE_DOC[stage])
    print(f"\n  {stage} — owning document(s): {docs}")
    for val, name, ln in sorted(by[stage]):
        print(f"     {val:>4}  {name}   (declared docs/compiler/diagnostic-system.md:{ln})")
tot = {}
for name, val, stage, ln in rows: tot[stage] = tot.get(stage, 0) + 1
print("\n" + "=" * 72)
print("Coverage per stage — the ratio is what makes a gap meaningful")
print("=" * 72)
for stage in sorted(tot):
    miss = len(by.get(stage, []))
    print(f"  {stage:<10} {tot[stage]-miss:>3} of {tot[stage]:>3} codes named in the stage document"
          f"   ({miss} not)")
print(f"\n  {len(rows)} codes attributed to a stage; {len(missing)} not found in the stage's own document.")
if unmapped:
    print(f"\n  Banners with no document in the map, whose codes were skipped: {sorted(unmapped)}")
print("\n  A stage whose document names most of its codes and misses a few is the signal.")
print("  A stage whose document names almost none never enumerated them, and its misses say nothing.")
