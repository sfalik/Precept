#!/usr/bin/env python3
"""
Deterministic calibration scorer.

Runs the compiled `runner` binary over calibration-cells.json (no LLM
anywhere in this path), then computes:

  C1 - RUNNER FIDELITY: runner error-code set == MCP-oracle error-code set
       recorded in calibration-cells.json, for every cell. Must be 100%.

  C2 - METHOD ACCURACY: methodVerdict (derived mechanically from the
       runner's error-code set vs expectedLabel) vs trueGapStatus (recorded
       in calibration-cells.json, itself derived from the MCP oracle using
       the identical decision rule). Confusion matrix + FP/FN list.

Code identity is normalized between the two representations the compiler
uses in the wild: the enum-name string (what Diagnostic.Code / the runner
emits) and the PRE#### numeric string (what the precept_compile MCP tool's
compact diagnostics use). diagnostic_code_map.tsv (extracted verbatim from
src/Precept/Language/DiagnosticCode.cs enum values) is the single source of
truth for that mapping -- no code-name guessing anywhere in this script.
"""
import json
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
CELLS_PATH = os.path.join(HERE, "calibration-cells.json")
RUNNER_DLL = os.path.join(HERE, "runner", "bin", "Debug", "net10.0", "runner.dll")
CODE_MAP_PATH = os.path.join(HERE, "diagnostic_code_map.tsv")
MANIFEST_PATH = os.path.join(HERE, "run-manifest.json")
RESULTS_PATH = os.path.join(HERE, "RESULTS.md")


def load_code_map():
    """Returns (pre_to_name, name_to_pre) dicts, e.g. pre_to_name['PRE0064'] = 'UnguardedCollectionMutation'."""
    pre_to_name = {}
    name_to_pre = {}
    with open(CODE_MAP_PATH) as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            num, name = line.split("\t")
            pre = f"PRE{int(num):04d}"
            pre_to_name[pre] = name
            name_to_pre[name] = pre
    return pre_to_name, name_to_pre


def normalize_code(code, pre_to_name):
    """Normalize any code representation (PRE#### or enum name) to the enum-name form."""
    if code.startswith("PRE") and code[3:].isdigit():
        return pre_to_name.get(code, code)
    return code


def parse_expected_label(label, pre_to_name):
    """
    Parses a cells.json-style expected label into (kind, normalized_code_or_None).
    kind in {"accept", "reject_any", "reject_code"}.
    """
    if label == "accept":
        return ("accept", None)
    if label == "reject:any":
        return ("reject_any", None)
    if label.startswith("reject:"):
        raw_code = label[len("reject:"):]
        return ("reject_code", normalize_code(raw_code, pre_to_name))
    raise ValueError(f"Unrecognized expectedLabel: {label!r}")


def method_verdict(error_codes_enum_names, expected_label, pre_to_name):
    """
    Deterministic decision rule (no model judgment):
      accept        -> not-gap iff error_codes is empty
      reject:any    -> not-gap iff error_codes is non-empty
      reject:CODE   -> not-gap iff CODE (normalized) is present in error_codes
    Returns "not-gap" or "gap".
    """
    kind, code = parse_expected_label(expected_label, pre_to_name)
    codes = set(error_codes_enum_names)
    if kind == "accept":
        matches = len(codes) == 0
    elif kind == "reject_any":
        matches = len(codes) > 0
    elif kind == "reject_code":
        matches = code in codes
    else:
        raise AssertionError(kind)
    return "not-gap" if matches else "gap"


def main():
    pre_to_name, name_to_pre = load_code_map()

    with open(CELLS_PATH) as f:
        cells = json.load(f)

    manifest = [{"id": c["id"], "text": c["text"]} for c in cells]
    with open(MANIFEST_PATH, "w") as f:
        json.dump(manifest, f, indent=2)

    proc = subprocess.run(
        ["dotnet", RUNNER_DLL, MANIFEST_PATH],
        capture_output=True, text=True, timeout=120,
    )
    if proc.returncode != 0:
        print("RUNNER FAILED", file=sys.stderr)
        print(proc.stdout, file=sys.stderr)
        print(proc.stderr, file=sys.stderr)
        sys.exit(1)

    runner_results = {r["id"]: r for r in json.loads(proc.stdout)}

    # ---------------- C1: runner fidelity vs MCP oracle ----------------
    c1_rows = []
    c1_mismatches = []
    for c in cells:
        cid = c["id"]
        oracle_codes = set(normalize_code(x, pre_to_name) for x in c["actualErrorCodes"])
        runner_codes = set(runner_results[cid]["errorCodes"])
        match = oracle_codes == runner_codes
        c1_rows.append((cid, oracle_codes, runner_codes, match))
        if not match:
            c1_mismatches.append((cid, oracle_codes, runner_codes))

    c1_total = len(cells)
    c1_pass = sum(1 for r in c1_rows if r[3])

    # ---------------- C2: method accuracy vs true gap status ----------------
    confusion = {"TN": [], "FP": [], "TP": [], "FN": []}
    c2_rows = []
    for c in cells:
        cid = c["id"]
        runner_codes = runner_results[cid]["errorCodes"]
        verdict = method_verdict(runner_codes, c["expectedLabel"], pre_to_name)
        true_status = c["trueGapStatus"]
        if true_status == "not-gap" and verdict == "not-gap":
            bucket = "TN"
        elif true_status == "not-gap" and verdict == "gap":
            bucket = "FP"
        elif true_status == "gap" and verdict == "gap":
            bucket = "TP"
        elif true_status == "gap" and verdict == "not-gap":
            bucket = "FN"
        else:
            raise AssertionError((true_status, verdict))
        confusion[bucket].append(cid)
        c2_rows.append((cid, c["expectedLabel"], runner_codes, verdict, true_status, bucket))

    tn, fp, tp, fn = len(confusion["TN"]), len(confusion["FP"]), len(confusion["TP"]), len(confusion["FN"])
    total_notgap = tn + fp
    total_gap = tp + fn
    fp_rate = (fp / total_notgap) if total_notgap else float("nan")
    fn_rate = (fn / total_gap) if total_gap else float("nan")

    # ---------------- console report ----------------
    print("=" * 78)
    print(f"C1 FIDELITY: {c1_pass}/{c1_total} cells where runner errorCodes == MCP oracle errorCodes")
    print("=" * 78)
    for cid, oc, rc, mismatch_codes in c1_mismatches:
        print(f"  MISMATCH {cid}")
        print(f"    oracle: {sorted(oc)}")
        print(f"    runner: {sorted(rc)}")

    print()
    print("=" * 78)
    print("C2 CONFUSION MATRIX")
    print("=" * 78)
    print(f"  TN={tn}  FP={fp}  TP={tp}  FN={fn}")
    print(f"  FP rate = {fp}/{total_notgap} = {fp_rate:.4f}" if total_notgap else "  FP rate = n/a (no not-gap cells)")
    print(f"  FN rate = {fn}/{total_gap} = {fn_rate:.4f}" if total_gap else "  FN rate = n/a (no gap cells)")
    print()
    if confusion["FP"]:
        print("  FALSE POSITIVES (method invents a gap):")
        for cid, exp, codes, verdict, truth, bucket in c2_rows:
            if bucket == "FP":
                print(f"    {cid}: expectedLabel={exp} actualCodes={codes} methodVerdict={verdict} trueGapStatus={truth}")
    if confusion["FN"]:
        print("  FALSE NEGATIVES (method misses a real gap):")
        for cid, exp, codes, verdict, truth, bucket in c2_rows:
            if bucket == "FN":
                print(f"    {cid}: expectedLabel={exp} actualCodes={codes} methodVerdict={verdict} trueGapStatus={truth}")

    # ---------------- RESULTS.md ----------------
    with open(RESULTS_PATH, "w") as f:
        f.write("# Calibration harness results\n\n")
        f.write(f"Total cells: {len(cells)}\n\n")
        f.write("## C1 — Runner fidelity vs MCP oracle\n\n")
        f.write(f"{c1_pass}/{c1_total} cells match.\n\n")
        if c1_mismatches:
            f.write("### Mismatches\n\n")
            f.write("| id | oracle codes | runner codes |\n|---|---|---|\n")
            for cid, oc, rc in c1_mismatches:
                f.write(f"| {cid} | {sorted(oc)} | {sorted(rc)} |\n")
        f.write("\n## C2 — Method accuracy confusion matrix\n\n")
        f.write(f"| | trueGapStatus=not-gap | trueGapStatus=gap |\n|---|---|---|\n")
        f.write(f"| methodVerdict=not-gap | TN={tn} | FN={fn} |\n")
        f.write(f"| methodVerdict=gap | FP={fp} | TP={tp} |\n\n")
        f.write(f"FP rate: {fp}/{total_notgap} = {fp_rate:.4f}\n\n" if total_notgap else "FP rate: n/a\n\n")
        f.write(f"FN rate: {fn}/{total_gap} = {fn_rate:.4f}\n\n" if total_gap else "FN rate: n/a\n\n")
        f.write("## Per-cell table\n\n")
        f.write("| id | source | expectedLabel | runner errorCodes | methodVerdict | trueGapStatus | bucket |\n")
        f.write("|---|---|---|---|---|---|---|\n")
        cells_by_id = {c["id"]: c for c in cells}
        for cid, exp, codes, verdict, truth, bucket in c2_rows:
            src = cells_by_id[cid]["source"]
            f.write(f"| {cid} | {src} | {exp} | {codes} | {verdict} | {truth} | {bucket} |\n")

    print()
    print(f"RESULTS.md written to {RESULTS_PATH}")

    verdict_pass = (c1_pass == c1_total)
    print()
    print("=" * 78)
    print(f"VERDICT: {'PASS' if verdict_pass else 'FAIL'} -- C1={c1_pass}/{c1_total}, "
          f"C2 FP={fp}/{total_notgap}, FN={fn}/{total_gap}")
    print("=" * 78)


if __name__ == "__main__":
    main()
