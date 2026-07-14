# Calibration harness results

Total cells: 29

## C1 — Runner fidelity vs MCP oracle

29/29 cells match.


## C2 — Method accuracy confusion matrix

| | trueGapStatus=not-gap | trueGapStatus=gap |
|---|---|---|
| methodVerdict=not-gap | TN=24 | FN=0 |
| methodVerdict=gap | FP=0 | TP=5 |

FP rate: 0/24 = 0.0000

FN rate: 0/5 = 0.0000

## Per-cell table

| id | source | expectedLabel | runner errorCodes | methodVerdict | trueGapStatus | bucket |
|---|---|---|---|---|---|---|
| A-customer-profile | A-sample | accept | [] | not-gap | not-gap | TN |
| A-invoice-line-item | A-sample | accept | [] | not-gap | not-gap | TN |
| A-crosswalk-signal | A-sample | accept | [] | not-gap | not-gap | TN |
| A-loan-application | A-sample | accept | [] | not-gap | not-gap | TN |
| A-insurance-claim | A-sample | accept | [] | not-gap | not-gap | TN |
| A-inventory-item | A-sample | accept | [] | not-gap | not-gap | TN |
| A-library-book-checkout | A-sample | accept | [] | not-gap | not-gap | TN |
| A-hiring-pipeline | A-sample | accept | [] | not-gap | not-gap | TN |
| A-event-registration | A-sample | accept | [] | not-gap | not-gap | TN |
| A-fee-schedule | A-sample | accept | [] | not-gap | not-gap | TN |
| A-clinic-appointment-scheduling | A-sample | accept | [] | not-gap | not-gap | TN |
| A-maintenance-work-order | A-sample | accept | [] | not-gap | not-gap | TN |
| A-payment-method | A-sample | accept | [] | not-gap | not-gap | TN |
| A-saas-subscription-billing | A-sample | accept | [] | not-gap | not-gap | TN |
| A-medical-device-tracking | A-sample | accept | [] | not-gap | not-gap | TN |
| B-1470-list-remove-at-guarded | B-probe | accept | ['UnguardedCollectionMutation'] | gap | gap | TP |
| B-1471-list-remove-at-unguarded | B-probe | reject:UnguardedCollectionAccess | ['IndexBoundsGuard', 'UnguardedCollectionMutation'] | gap | gap | TP |
| B-1472-list-clear | B-probe | accept | [] | not-gap | not-gap | TN |
| B-1483-list-set-eq-wrong | B-probe | reject:ScalarOperationOnCollection | ['TypeMismatch'] | gap | gap | TP |
| B-1800-stack-contains | B-probe | accept | [] | not-gap | not-gap | TN |
| B-1803-tildestring-stack-contains-ci | B-probe | accept | [] | not-gap | not-gap | TN |
| C1-cross-dimension-quantity-comparison | C-handauthored | reject:UnprovedQualifierCompatibility | ['UnprovedQualifierCompatibility'] | not-gap | not-gap | TN |
| C2-decimal-number-implicit-mix | C-handauthored | reject:TypeMismatch | ['TypeMismatch'] | not-gap | not-gap | TN |
| C3a-set-min-guarded | C-handauthored | accept | [] | not-gap | not-gap | TN |
| C3b-set-min-unguarded | C-handauthored | reject:UnguardedCollectionAccess | ['UnguardedCollectionAccess'] | not-gap | not-gap | TN |
| C4-divide-by-literal-zero | C-handauthored | reject:any | ['DivisionByZero'] | not-gap | not-gap | TN |
| C5-clean-stateful-construction | C-handauthored | accept | [] | not-gap | not-gap | TN |
| C6-unqualified-money-min-KNOWN-GAP | C-handauthored | reject:any | [] | gap | gap | TP |
| C7-floor-on-money-KNOWN-GAP | C-handauthored | accept | ['TypeMismatch'] | gap | gap | TP |
