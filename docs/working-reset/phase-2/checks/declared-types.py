# Canon-against-canon check 1: every type declared in more than one canonical document,
# compared member by member.
#
# Run from the repository root:  python3 docs/working-reset/phase-2/checks/declared-types.py
#
# What it does. Walks every fenced code block in the canonical set, extracts each type
# declaration and its member names (primary-constructor parameters, property declarations,
# enum members), groups the declarations by type name, and prints every name whose member
# sets are not identical across the documents that declare it.
#
# What it cannot do. It cannot tell a genuine disagreement from a deliberately abbreviated
# view. A declaration carrying an elision marker is flagged [elided]; everything else has to
# be read. Triage of the 2026-08-27 run is written up beside this file.
import re, pathlib, collections, json
roots = ["docs/language","docs/compiler","docs/runtime","docs/tooling"]
files = [p for r in roots for p in pathlib.Path(r).rglob("*.md")]
files += [pathlib.Path("docs/compiler-and-runtime-design.md"), pathlib.Path("README.md")]
files = [f for f in files if "archive" not in str(f)]

declre = re.compile(r'^(\s*)(?:public\s+|internal\s+|sealed\s+|abstract\s+|static\s+|partial\s+|readonly\s+|ref\s+)*'
                    r'(record struct|record|class|struct|enum|interface)\s+([A-Z][A-Za-z0-9_]*)')
propre = re.compile(r'^\s*(?:public\s+|internal\s+|required\s+|static\s+|readonly\s+|init\s+)*'
                    r'[A-Za-z_][\w<>,\[\]\?\. ]*\s+([A-Z][A-Za-z0-9_]*)\s*(?:\{\s*get|;|=)')
elide = re.compile(r'\.\.\.|…')

def params(sig):
    # text inside the outermost (...) of a primary constructor, possibly spanning lines
    d, out, started = 0, [], False
    for ch in sig:
        if ch == '(':
            d += 1; started = True
            if d == 1: continue
        elif ch == ')':
            d -= 1
            if d == 0: break
        if started and d >= 1: out.append(ch)
    inner = "".join(out)
    if not inner.strip(): return []
    parts, depth, cur = [], 0, ""
    for ch in inner:
        if ch in "<([": depth += 1
        elif ch in ">)]": depth -= 1
        if ch == ',' and depth == 0:
            parts.append(cur); cur = ""
        else: cur += ch
    parts.append(cur)
    names = []
    for p in parts:
        p = p.split('=')[0].strip()
        if not p: continue
        toks = p.replace('\n',' ').split()
        if toks and re.fullmatch(r"[A-Z][A-Za-z0-9_]*", toks[-1]): names.append(toks[-1])
    return names

decls = collections.defaultdict(list)
for f in files:
    lines = [re.sub(r"//.*$", "", x) for x in f.read_text().splitlines()]
    infence, fence_start = False, 0
    for i, l in enumerate(lines):
        if l.lstrip().startswith("```"):
            infence = not infence; continue
        if not infence: continue
        m = declre.match(l)
        if not m: continue
        indent, kind, name = len(m.group(1)), m.group(2), m.group(3)
        # gather the declaration block: from here to the matching close, or to a decl at <= indent
        blk, depth, j = [], 0, i
        sig = ""
        while j < len(lines):
            ln = lines[j]
            if ln.lstrip().startswith("```"): break
            if j == i or (depth == 0 and not sig.count('(') == sig.count(')')):
                sig += " " + ln
            blk.append(ln)
            depth += ln.count('{') - ln.count('}')
            if j > i and depth <= 0 and ('{' in "".join(blk) or ln.rstrip().endswith(';')):
                break
            if j > i:
                m2 = declre.match(ln)
                if m2 and len(m2.group(1)) <= indent: 
                    blk.pop(); break
            j += 1
        body = "\n".join(blk)
        mem = params(sig) if '(' in sig.split('{')[0] else []
        if kind == 'enum':
            inner = body[body.find('{')+1:]
            mem = [x.strip().split('=')[0].strip() for x in inner.replace('}','').split(',')]
            mem = [x for x in mem if re.fullmatch(r'[A-Z][A-Za-z0-9_]*', x)]
        else:
            for ln in blk[1:]:
                pm = propre.match(ln)
                if pm and pm.group(1) not in mem and re.fullmatch(r"[A-Z][A-Za-z0-9_]*", pm.group(1)): mem.append(pm.group(1))
        decls[name].append({"file": str(f), "line": i+1, "kind": kind,
                            "members": mem, "elided": bool(elide.search(body))})

out = {}
for name, ds in decls.items():
    byfile = collections.defaultdict(list)
    for d in ds: byfile[d["file"]].append(d)
    if len(byfile) < 2: continue
    sets = {f: set(m for d in v for m in d["members"]) for f, v in byfile.items()}
    if any(not s for s in sets.values()): continue
    if len(set(map(frozenset, sets.values()))) > 1:
        out[name] = {f: {"line": [d["line"] for d in v], "members": sorted(sets[f]),
                         "elided": any(d["elided"] for d in v)} for f, v in byfile.items()}
import os
# Only writes the machine-readable form when OUT is set, so a plain run leaves no files behind.
if os.environ.get("OUT"): json.dump(out, open(os.environ["OUT"], "w"), indent=1)
print(f"types declared in 2+ documents with a NON-IDENTICAL member set: {len(out)}\n")
for n in sorted(out):
    fs = out[n]
    allm = set().union(*[set(v["members"]) for v in fs.values()])
    common = set.intersection(*[set(v["members"]) for v in fs.values()])
    if allm == common: continue
    print(f"### {n}")
    for f, v in fs.items():
        missing = sorted(allm - set(v["members"]))
        print(f"   {f}:{','.join(map(str,v['line']))}{'  [elided]' if v['elided'] else ''}")
        if missing: print(f"      lacks: {', '.join(missing)}")
    print()
