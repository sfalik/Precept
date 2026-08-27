# Canon-against-canon check 2: counts a document states about a set, against the set
# and against every other document's count of the same thing.
#
# Run from the repository root:  python3 docs/working-reset/phase-2/checks/stated-counts.py
#
# Two reports.
#   A. Declared enums whose member count disagrees with a number stated near their name.
#   B. The same countable noun given different numbers in different places.
#
# What it cannot do. A noun can legitimately carry different numbers in different scopes
# ("five states" in one example and "three states" in another). Report B is a candidate
# list to read, not a defect list.
import re, pathlib, collections

roots = ["docs/language","docs/compiler","docs/runtime","docs/tooling"]
files = [p for r in roots for p in pathlib.Path(r).rglob("*.md")]
files += [pathlib.Path("docs/compiler-and-runtime-design.md"), pathlib.Path("README.md"),
          pathlib.Path("docs/philosophy.md")]
files = sorted({f for f in files if f.exists() and "archive" not in str(f)})

WORDS = {"zero":0,"one":1,"two":2,"three":3,"four":4,"five":5,"six":6,"seven":7,"eight":8,
         "nine":9,"ten":10,"eleven":11,"twelve":12,"thirteen":13,"fourteen":14,"fifteen":15,
         "sixteen":16,"seventeen":17,"eighteen":18,"nineteen":19,"twenty":20,"thirty":30,
         "forty":40,"fifty":50,"sixty":60,"seventy":70,"eighty":80,"ninety":90}

def num(tok):
    t = tok.lower().replace(",","")
    if t.isdigit(): return int(t)
    if t in WORDS: return WORDS[t]
    if "-" in t:
        a, _, b = t.partition("-")
        if a in WORDS and b in WORDS: return WORDS[a] + WORDS[b]
    return None

# ---- A. enum member counts against numbers stated near the enum name -------------------
enumre = re.compile(r'^\s*(?:public\s+|internal\s+)?enum\s+([A-Z][A-Za-z0-9_]*)')
enums = {}
for f in files:
    lines = f.read_text().splitlines()
    infence = False
    for i, l in enumerate(lines):
        if l.lstrip().startswith("```"):
            infence = not infence; continue
        if not infence: continue
        m = enumre.match(l)
        if not m: continue
        body, depth, j = "", 0, i
        while j < len(lines) and not lines[j].lstrip().startswith("```"):
            body += lines[j] + "\n"
            depth += lines[j].count("{") - lines[j].count("}")
            if j > i and depth <= 0 and "{" in body: break
            j += 1
        inner = body[body.find("{")+1:]
        inner = re.sub(r'//.*', '', inner)
        mem = [x.strip().split("=")[0].strip() for x in inner.replace("}", "").split(",")]
        mem = [x for x in mem if re.fullmatch(r'[A-Z][A-Za-z0-9_]*', x)]
        if mem:
            enums.setdefault(m.group(1), []).append((str(f), i+1, len(mem)))

# De-camel an enum name to the phrase prose uses for it: DiagnosticCode -> "diagnostic code".
def phrase(name):
    return " ".join(re.findall(r'[A-Z][a-z0-9]*', name)).lower()

print("=" * 72)
print("A. Enum member counts against the numbers canon states for them")
print("=" * 72)
alltext = {str(f): f.read_text().splitlines() for f in files}
hitsA = 0
for name, decls in sorted(enums.items()):
    counts = sorted({c for _, _, c in decls})
    ph = phrase(name)
    if not ph: continue
    pat = re.compile(r'\b([0-9]{1,4}|[A-Za-z\-]+)\s+(?:active\s+|declared\s+|distinct\s+|total\s+|possible\s+)?'
                     + re.escape(ph) + r's?\b', re.I)
    stated = []
    for f, lines in alltext.items():
        for i, l in enumerate(lines, 1):
            if l.lstrip().startswith(("public ", "internal ", "sealed ")): continue
            for m in pat.finditer(l):
                # A section reference is not a count: skip a number that is part of
                # "§7.5", "#75-access-mode" or any dotted or hashed sequence.
                if m.start() > 0 and l[m.start()-1] in ".#\u00a7-": continue
                n = num(m.group(1))
                if n is None or n < 2 or n > 2000: continue
                stated.append((n, f, i, l.strip()[:150]))
    if not stated: continue
    bad = [x for x in stated if x[0] not in counts]
    if not bad: continue
    print(f"\n  {name} — declared with {counts} member(s): "
          f"{', '.join(f'{a}:{b}' for a, b, _ in decls)}")
    for n, f, i, txt in sorted(set(bad)):
        print(f"     states {n:>5}  {f}:{i}")
        print(f"                   | {txt}")
        hitsA += 1
print(f"\n  {hitsA} to read.\n")

# ---- B. the same countable noun given different numbers --------------------------------
NOUNS = ["diagnostic codes","roslyn rules","proof requirement kinds","obligation kinds",
         "fault codes","premise classes","constraint kinds","modifier kinds","token kinds",
         "activation anchors","anchor buckets","proof strategies","pipeline stages",
         "passes","catalogs","descriptor types","outcome variants","variants","members"]
byn = collections.defaultdict(list)
for f in files:
    for i, l in enumerate(f.read_text().splitlines(), 1):
        low = l.lower()
        for nphrase in NOUNS:
            for m in re.finditer(r'\b([0-9]{1,4}|[A-Za-z\-]+)\s+(?:active\s+|distinct\s+|declared\s+|total\s+)?'
                                 + re.escape(nphrase), low):
                n = num(m.group(1))
                if n is None or n < 2 or n > 2000: continue
                byn[nphrase].append((n, str(f), i, l.strip()[:150]))
print("=" * 72)
print("B. The same countable noun given different numbers")
print("=" * 72)
hitsB = 0
for nphrase, rows in sorted(byn.items()):
    nums = {n for n, _, _, _ in rows}
    if len(nums) < 2: continue
    print(f"\n  '{nphrase}' — stated as {sorted(nums)}")
    for n, f, i, txt in sorted(rows):
        print(f"     {n:>5}  {f}:{i}")
        print(f"            | {txt}")
        hitsB += 1
print(f"\n  {hitsB} lines across {sum(1 for v in byn.values() if len({n for n,_,_,_ in v})>1)} nouns.")
