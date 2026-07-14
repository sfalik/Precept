#!/usr/bin/env python3
"""Builds calibration-cells.json from source texts + MCP-oracle-observed codes.

All 'text' fields are read verbatim from the source .precept files (source A/B)
or from the hand-authored snippets below (source C). All 'actualErrorCodes' /
'trueGapStatus' fields are transcribed directly from the precept_compile MCP
tool calls executed against these exact texts earlier in this session — see
the session transcript for verbatim MCP JSON responses per cell.
"""
import json
import os

SAMPLES = "/home/sfalik/source/repos/Precept/samples"


def read(name):
    with open(os.path.join(SAMPLES, name + ".precept")) as f:
        return f.read()


cells = []

# ---------------------------------------------------------------------------
# Source A — 15 real samples/*.precept files, verbatim. All must be not-gap
# (accept, zero error-severity codes, zero PRE0093/PRE0158-as-error noise).
# ---------------------------------------------------------------------------
A_FILES = [
    "customer-profile", "invoice-line-item", "crosswalk-signal",
    "loan-application", "insurance-claim", "inventory-item",
    "library-book-checkout", "hiring-pipeline", "event-registration",
    "fee-schedule", "clinic-appointment-scheduling", "maintenance-work-order",
    "payment-method", "saas-subscription-billing", "medical-device-tracking",
]
for name in A_FILES:
    cells.append({
        "id": f"A-{name}",
        "source": "A-sample",
        "text": read(name),
        "expectedLabel": "accept",
        "trueGapStatus": "not-gap",
        "actualErrorCodes": [],
        "note": "MCP precept_compile: diagnosticCount=0, diagnostics=[]. Zero errors, zero warnings, no PRE0093/PRE0158 noise.",
    })

# ---------------------------------------------------------------------------
# Source B — salvaged complete probe defs from probes/*.precept, cross-
# referenced to cells.json exercises by structural match (see report for the
# match rationale; probes that could not be confidently mapped were skipped).
# ---------------------------------------------------------------------------
cells.append({
    "id": "B-1470-list-remove-at-guarded",
    "source": "B-probe",
    "cellsJsonId": "list/action/remove-at-guarded-accept",
    "text": """precept Test1470

field Items as list of string

state Active initial

event RemoveAt(Position as integer)

from Active on RemoveAt when RemoveAt.Position >= 0 and RemoveAt.Position < Items.count
    -> remove Items at RemoveAt.Position
    -> no transition
""",
    "expectedLabel": "accept",
    "trueGapStatus": "gap",
    "actualErrorCodes": ["UnguardedCollectionMutation"],
    "note": "cells.json expects accept (guard N>=0 and N<count implies count>0 mathematically). "
            "MCP actual: 1 error PRE0064/UnguardedCollectionMutation ('List must be non-empty' obligation left Unresolved). "
            "The proof engine does not derive count>0 from N>=0 and N<count -- a real narrowing incompleteness, not a harness artifact.",
})
cells.append({
    "id": "B-1471-list-remove-at-unguarded",
    "source": "B-probe",
    "cellsJsonId": "list/action/remove-at-unguarded-reject",
    "text": """precept Test1471

field Items as list of string

state Active initial

event RemoveAt(Position as integer)

from Active on RemoveAt
    -> remove Items at RemoveAt.Position
    -> no transition
""",
    "expectedLabel": "reject:UnguardedCollectionAccess",
    "trueGapStatus": "gap",
    "actualErrorCodes": ["UnguardedCollectionMutation", "IndexBoundsGuard"],
    "note": "cells.json names UnguardedCollectionAccess (PRE0063); actual compiler fires "
            "UnguardedCollectionMutation (PRE0064) + IndexBoundsGuard (PRE0100) instead -- correct reject "
            "direction, different code family (Access=read accessors, Mutation=write actions per DiagnosticCode.cs). "
            "A cells.json labeling/doc-drift issue, not a scorer bug -- confirmed by C3b below where "
            "UnguardedCollectionAccess DOES fire exactly for a read accessor (.min).",
})
cells.append({
    "id": "B-1472-list-clear",
    "source": "B-probe",
    "cellsJsonId": "list/action/clear-accept",
    "text": """precept Test1472

field Items as list of string

state Active initial

event Reset

from Active on Reset
    -> clear Items
    -> no transition
""",
    "expectedLabel": "accept",
    "trueGapStatus": "not-gap",
    "actualErrorCodes": [],
    "note": "MCP actual: 0 errors (1 warning FieldNeverSet/PRE0158, not error-severity).",
})
cells.append({
    "id": "B-1483-list-set-eq-wrong",
    "source": "B-probe",
    "cellsJsonId": "list/action/set-eq-wrong",
    "text": """precept Test1483

field Items as list of string

state Active initial

event Replace(Value as string)

from Active on Replace
    -> set Items = Replace.Value
    -> no transition
""",
    "expectedLabel": "reject:ScalarOperationOnCollection",
    "trueGapStatus": "gap",
    "actualErrorCodes": ["TypeMismatch"],
    "note": "cells.json names ScalarOperationOnCollection (PRE0048); actual fires generic TypeMismatch "
            "(PRE0018, 'Expected a list value here, but got string') instead. Correct reject direction, "
            "wrong/generic code -- matches the already-ledgered G46 pattern (scalar set= on a collection "
            "falls through to a generic code).",
})
cells.append({
    "id": "B-1800-stack-contains",
    "source": "B-probe",
    "cellsJsonId": "membership/stack-contains-accept",
    "text": """precept Probe

field RepairSteps as stack of string

state Draft initial
state Done terminal

event Go

from Draft on Go when RepairSteps contains "step-1"
    -> transition Done
""",
    "expectedLabel": "accept",
    "trueGapStatus": "not-gap",
    "actualErrorCodes": [],
    "note": "MCP actual: 0 errors (1 warning FieldNeverSet/PRE0158, not error-severity).",
})
cells.append({
    "id": "B-1803-tildestring-stack-contains-ci",
    "source": "B-probe",
    "cellsJsonId": "innertype/tildestring-stack-contains-ci-accept",
    "text": """precept Probe

field BorrowerHistory as stack of ~string

state Draft initial
state Done terminal

event Go

from Draft on Go when BorrowerHistory contains "APPLE"
    -> transition Done
""",
    "expectedLabel": "accept",
    "trueGapStatus": "not-gap",
    "actualErrorCodes": [],
    "note": "MCP actual: 0 errors (1 warning FieldNeverSet/PRE0158, not error-severity).",
})

# ---------------------------------------------------------------------------
# Source C — hand-authored, ledger-anchored cells with known correct
# behavior, established via the MCP oracle.
# ---------------------------------------------------------------------------
cells.append({
    "id": "C1-cross-dimension-quantity-comparison",
    "source": "C-handauthored",
    "text": """precept CalibC1

field Weight as quantity in 'kg' default '1 kg'
field Length as quantity in 'm' default '1 m'

rule Weight < Length because "cross-dimension quantity comparison must be rejected"
""",
    "expectedLabel": "reject:UnprovedQualifierCompatibility",
    "trueGapStatus": "not-gap",
    "actualErrorCodes": ["UnprovedQualifierCompatibility"],
    "note": "MCP actual: 1 error PRE0114/UnprovedQualifierCompatibility "
            "(\"Cannot combine 'Weight' (Unit: 'kg') and 'Length' (Unit: 'm')\"). "
            "Compiler correctly rejects -- this is the case the OLD probe method wrongly flagged as "
            "'compiles clean' = gap; the corrected calibration confirms not-gap.",
})
cells.append({
    "id": "C2-decimal-number-implicit-mix",
    "source": "C-handauthored",
    "text": """precept CalibC2

field A as decimal default 1.0
field B as number default 1.0

rule A + B > 0 because "decimal/number implicit mix"
""",
    "expectedLabel": "reject:TypeMismatch",
    "trueGapStatus": "not-gap",
    "actualErrorCodes": ["TypeMismatch"],
    "note": "MCP actual: 1 error PRE0018/TypeMismatch (\"Expected a decimal value here, but got 'number'\").",
})
cells.append({
    "id": "C3a-set-min-guarded",
    "source": "C-handauthored",
    "text": """precept CalibC3a

field Scores as set of integer
field X as integer optional

state Draft initial
event Go

from Draft on Go when Scores.count > 0
    -> set X = Scores.min
    -> no transition
""",
    "expectedLabel": "accept",
    "trueGapStatus": "not-gap",
    "actualErrorCodes": [],
    "note": "MCP actual: 0 errors. Guard count>0 discharges the .min emptiness obligation (Proved/GuardInPath).",
})
cells.append({
    "id": "C3b-set-min-unguarded",
    "source": "C-handauthored",
    "text": """precept CalibC3b

field Scores as set of integer
field X as integer optional

state Draft initial
event Go

from Draft on Go
    -> set X = Scores.min
    -> no transition
""",
    "expectedLabel": "reject:UnguardedCollectionAccess",
    "trueGapStatus": "not-gap",
    "actualErrorCodes": ["UnguardedCollectionAccess"],
    "note": "MCP actual: 1 error PRE0063/UnguardedCollectionAccess exactly. Confirms Access is the correct "
            "code family for unguarded READ accessors (contrast with B-1471/B-1483 where write actions "
            "fire Mutation/generic codes instead of the cells.json-expected Access/Scalar codes).",
})
cells.append({
    "id": "C4-divide-by-literal-zero",
    "source": "C-handauthored",
    "text": """precept CalibC4

field A as integer default 10
field Ratio as decimal <- A / 0
""",
    "expectedLabel": "reject:any",
    "trueGapStatus": "not-gap",
    "actualErrorCodes": ["DivisionByZero"],
    "note": "MCP actual: 1 error PRE0083/DivisionByZero (\"Division is unsafe: '0' can be zero\").",
})
cells.append({
    "id": "C5-clean-stateful-construction",
    "source": "C-handauthored",
    "text": """precept CalibC5

field Name as string
field Amount as money in 'USD'

state Open initial

event Create(Name as string notempty, Amount as money in 'USD' positive) initial

on Create
    -> set Name = Create.Name
    -> set Amount = Create.Amount
""",
    "expectedLabel": "accept",
    "trueGapStatus": "not-gap",
    "actualErrorCodes": [],
    "note": "MCP actual: 0 errors (1 warning PRE0119/StructuralSinkState -- 'Open' has no outgoing "
            "transitions; harmless for this construction-only smoke test, not error-severity).",
})
cells.append({
    "id": "C6-unqualified-money-min-KNOWN-GAP",
    "source": "C-handauthored",
    "text": """precept CalibC6

field Payments as set of money
field X as money optional

state Draft initial
event Go

from Draft on Go when Payments.count > 0
    -> set X = Payments.min
    -> no transition
""",
    "expectedLabel": "reject:any",
    "trueGapStatus": "gap",
    "actualErrorCodes": [],
    "note": "Ledger G45: unqualified money/quantity/price .min/.max should be a type error (cross-currency "
            "ordering undefined) but compiles clean. MCP actual: 0 errors -- confirms the known soundness gap live.",
})
cells.append({
    "id": "C7-floor-on-money-KNOWN-GAP",
    "source": "C-handauthored",
    "text": """precept CalibC7

field Price as money in 'USD' default '9.99 USD'
field Rounded as money in 'USD' <- floor(Price)
""",
    "expectedLabel": "accept",
    "trueGapStatus": "gap",
    "actualErrorCodes": ["TypeMismatch"],
    "note": "Ledger G40: floor/ceil/truncate should be accepted on money/quantity per spec D16 (inherited "
            "numeric operations) but the compiler rejects. MCP actual: 1 error PRE0018/TypeMismatch "
            "(\"Expected a floor value here, but got 'money'\") -- confirms the known over-rejection gap live, "
            "opposite direction from C6.",
})

out_path = os.path.join(os.path.dirname(__file__), "calibration-cells.json")
with open(out_path, "w") as f:
    json.dump(cells, f, indent=2)

print(f"wrote {len(cells)} cells to {out_path}")
by_source = {}
for c in cells:
    by_source.setdefault(c["source"], 0)
    by_source[c["source"]] += 1
print(by_source)
