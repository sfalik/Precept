# Precept 🛡️

[![NuGet Badge](https://img.shields.io/nuget/v/Precept)](https://www.nuget.org/packages/Precept)
[![Build Status](https://img.shields.io/github/actions/workflow/status/OwnerName/Precept/build.yml?branch=main)](https://github.com/OwnerName/Precept/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![VS Code Extension](https://img.shields.io/visual-studio-marketplace/v/OwnerName.precept-vscode?label=VS%20Code)](https://marketplace.visualstudio.com/items?itemName=OwnerName.precept-vscode)

> **pre·​cept** *(noun)*: A general rule intended to regulate behavior or thought; a strict command or principle of action.

**Precept is a domain integrity engine for .NET.** It binds an entity's state, data, and business rules into a single, executable contract. By treating your business constraints as unbreakable *precepts*, the engine ensures that invalid states and illegal data mutations are fundamentally impossible.

---

## 🚀 Quick Start

1. **Install the .NET Package:**
   ```bash
   dotnet add package Precept
   ```
2. **Install the VS Code Extension:** Search for `Precept DSL` in the marketplace or run:
   ```bash
   code --install-extension AuthorName.precept-vscode
   ```
3. **Write your first Precept file (`bank-loan.precept`):**
   *(See the code example below)*

---

## 💡 The "Aha!" Moment

With Precept, you define the rules of your entity in a clean, domain-readable DSL, and then execute those exact rules deterministically in C#.

### 1. The Constitution (`bank-loan.precept`)
```text
machine BankLoan

// 1. Data bound to the lifecycle
number RequestedAmount = 0
number ApprovedAmount = 0
number CreditScore = 0
number RemainingBalance = 0
	rule RemainingBalance >= 0 "Remaining balance cannot be negative"

// Top level rule evaluating cross-field requirements
rule ApprovedAmount <= RequestedAmount "Approved amount must not exceed requested amount"

// 2. Clear state progression
state Apply initial
state UnderReview
state Approved

// 3. Explicit Events and Arguments
event Submit
	number Amount
		rule Amount > 0 "Amount must be positive"
	number CreditScore
		rule CreditScore >= 300 "Credit score must be at least 300"

event Approve
	number ApprovedAmount
		rule ApprovedAmount > 0 "Approved amount must be positive"

// 4. Transitions & Guards
from Apply on Submit
	set RequestedAmount = Submit.Amount
	set CreditScore = Submit.CreditScore
	transition UnderReview

from UnderReview on Approve
	// Pure guard evaluations enforce the business rules strictly
	if CreditScore >= 700 && Approve.ApprovedAmount <= RequestedAmount
		set ApprovedAmount = Approve.ApprovedAmount
		set RemainingBalance = Approve.ApprovedAmount
		transition Approved
	else
		reject "Approval requires credit score >= 700 and valid amount"
```

### 2. The Execution (C#)
Because rules are absolute, and expressions are pure, you can safely `Inspect` an action before mutating your database. 

```csharp
using Precept.Runtime;

// Load the compiled precepts and the database record
var machine = PreceptDefinition.Load("bank-loan.precept");
var instance = machine.CreateInstance(jsonState);

// Safely inspect - without mutating!
var preview = instance.Inspect("Approve", new { ApprovedAmount = 100000 });

if (preview is Blocked b) 
{
    // Output: "Approval requires credit score >= 700 and valid amount"
    Console.WriteLine($"Cannot approve: {b.Reason}"); 
}
else if (preview is Enabled e)
{
    // The engine guarantees atoms: rules passed, state shifted, data updated.
    instance.Fire("Approve", new { ApprovedAmount = 100000 });
    await repository.SaveAsync(instance.StateData);
}
```

---

## 🛠️ World-Class Tooling

Precept isn't just a library; it's an authoring experience. The accompanying VS Code extension provides:
- **Interactive Inspector:** Fire events and edit data directly against a live, mock instance in VS Code to prove your rules function exactly as desired.
- **Live Diagramming:** A dynamic state-transition diagram renders as you type.
- **Null-Flow Analysis:** Real-time squiggles warn you if a guard path might access an unsafe null value.

![Interactive Inspector](docs/images/inspector-preview.png)

---

## 🧠 The Problem It Solves

Most complex entities start simple. But as business requirements grow, the rules governing their lifecycles scatter across your codebase:
- **State transitions** land in `switch` statements or scattered handler logic.
- **Data validation** gets pushed into ORMs, FluentValidation, or entity constructors.
- **Side effects** trigger asynchronously with no guarantee the data is ready.

Eventually, the system drifts. An entity ends up in a `Shipped` state without a `TrackingNumber`. When stakeholders ask, "Under what exact conditions can an Order be refunded?", developers have to traverse six different classes to find the answer.

Precept fixes this by treating the lifecycle of an entity as an executable contract.

## 🏗️ The Pillars of Precept

### 1. The Universal Safety Net (`rule`)
In most systems, validation is bound to *actions* (e.g., "Validate this API payload"). In Precept, rules are bound to the *data itself*. 

When you declare `rule Balance >= 0 "Balance cannot be negative"`, that precept is absolute. Whether a complex workflow transition deducts from the balance, or a user directly edits a linked field via an administrative override, the engine enforces the rule upon completion. If the rule fails, the entire transaction rolls back.

### 2. Pure Inspection (`Inspect` before `Fire`)
Because Precept enforces rigorous grammar constraints—expressions evaluate, statements mutate—it is impossible for a transition guard to accidentally mutate data. This allows the `Inspect` API to safely preview any action, returning a precise outcome with specific error reasons—all without saving a thing.

### 3. Atomic, Deterministic Mutations
A Precept transition either completely succeeds or entirely rolls back. Every evaluation is deterministic: the same definitions and the same data will *always* result in the same outcome. 

### 4. Two Mutational Paths
Precept acknowledges that entirely different ceremonies apply to different types of data updates:
* **Transitions (`event`):** For lifecycle changes where routing, auditing, and complex state progression matter.
* **Direct Edits (`edit`):** For simple data mutations where event ceremony is overkill. 

Both paths are safely watched by the exact same `rule` engine. Direct editing isn't a hack; it is a first-class feature protected by the same ironclad invariants.