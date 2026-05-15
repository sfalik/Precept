using System.Globalization;
using System.Text;
using Precept.Language;

namespace Precept.Mcp.Tools;

internal static class CatalogFormatters
{
    public static string FormatQuickstart()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Precept Quickstart");
        AppendSection(sb, "What Precept Is");
        sb.AppendLine(QuickstartCatalog.WhatIsPrecept);
        AppendSection(sb, "Core Guarantee");
        sb.AppendLine(QuickstartCatalog.CoreGuarantee);
        AppendSection(sb, "Core Concepts");

        foreach (var concept in QuickstartCatalog.CoreConcepts)
        {
            sb.Append("- **").Append(concept.Name).Append("** — ").Append(concept.Summary)
                .Append(" Example: `").Append(concept.Example).AppendLine("`.");
        }

        AppendSection(sb, "Tool Guide");
        foreach (var tool in QuickstartCatalog.ToolGuide)
        {
            sb.Append("- **").Append(tool.ToolName).Append("** — ").Append(tool.WhenToCall)
                .Append(" Returns: ").Append(tool.ReturnsSummary).AppendLine();
        }

        AppendSection(sb, "Minimal Examples");
        foreach (var example in QuickstartCatalog.MinimalExamples)
        {
            sb.Append("### ").AppendLine(example.Title);
            sb.AppendLine(example.Description);
            AppendCodeBlock(sb, example.DslSnippet);
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatSyntax()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Precept Syntax Reference");

        AppendSection(sb, "Grammar Rules");
        AppendBullet(sb, $"**Grammar model** — {SyntaxReference.GrammarModel}");
        AppendBullet(sb, $"**Comments** — {SyntaxReference.CommentSyntax}");
        AppendBullet(sb, $"**Identifiers** — {SyntaxReference.IdentifierRules}");
        AppendBullet(sb, $"**Strings** — {SyntaxReference.StringLiteralRules}");
        AppendBullet(sb, $"**Numbers** — {SyntaxReference.NumberLiteralRules}");
        AppendBullet(sb, $"**Whitespace** — {SyntaxReference.WhitespaceRules}");
        AppendBullet(sb, $"**Null narrowing** — {SyntaxReference.NullNarrowing}");
        AppendBullet(sb, $"**Typed constants** — {CollapseWhitespace(SyntaxReference.TypedConstantRules)}");
        AppendBullet(sb, $"**Expressions** — {CollapseWhitespace(SyntaxReference.ExpressionRules)}");

        AppendSection(sb, "Operator Precedence");
        foreach (var entry in SyntaxReference.PrecedenceTable)
        {
            AppendBullet(sb, entry);
        }

        AppendSection(sb, "Conventional Order");
        foreach (var entry in SyntaxReference.ConventionalOrder)
        {
            AppendBullet(sb, entry);
        }

        AppendSection(sb, "Constructs");
        foreach (var construct in Constructs.All)
        {
            var slots = construct.Slots.Select(RenderConstructSlot);
            var entries = construct.Entries.Select(RenderDisambiguationEntry);
            var line = new StringBuilder()
                .Append("- **").Append(construct.Name).Append("** (`").Append(construct.Kind).Append("`) — ")
                .Append(construct.Description)
                .Append(" Lead: ").Append(RenderToken(construct.PrimaryLeadingToken)).Append('.')
                .Append(" Allowed in: ").Append(RenderConstructScopes(construct.AllowedIn)).Append('.')
                .Append(" Slots: ").Append(JoinOrNone(slots)).Append('.')
                .Append(" Disambiguation: ").Append(JoinOrNone(entries)).Append('.')
                .Append(" Routing: `").Append(construct.RoutingFamily).Append("`.")
                .Append(" Modifier domain: `").Append(construct.ModifierDomain).Append("`.")
                .Append(" Example: `").Append(construct.UsageExample).Append("`.");

            if (!string.IsNullOrWhiteSpace(construct.SnippetTemplate))
            {
                line.Append(" Snippet: `").Append(construct.SnippetTemplate).Append("`.");
            }

            sb.AppendLine(line.ToString());
        }

        AppendSection(sb, "Actions");
        foreach (var action in Actions.All)
        {
            var line = new StringBuilder()
                .Append("- **").Append(action.Token.Text ?? action.Kind.ToString()).Append("** (`").Append(action.Kind).Append("`) — ")
                .Append(action.Description)
                .Append(" Applies to: ").Append(RenderTargets(action.ApplicableTo)).Append('.')
                .Append(" Allowed in: ").Append(JoinOrNone(action.AllowedIn.Select(kind => $"`{kind}`"))).Append('.')
                .Append(" Syntax: `").Append(action.SyntaxShape).Append("`.")
                .Append(" Value required: ").Append(RenderBool(action.ValueRequired)).Append('.')
                .Append(" Into supported: ").Append(RenderBool(Actions.GetShapeMeta(action.SyntaxShape).Slots.Any(slot => slot.Role == ActionSlotRole.IntoTarget))).Append('.');

            if (action.PrimaryActionKind is { } primary)
            {
                line.Append(" Primary action: `").Append(primary).Append("`.");
            }

            if (action.ProofRequirements.Length > 0)
            {
                line.Append(" Proofs: ").Append(JoinOrNone(action.ProofRequirements.Select(RenderProofRequirement))).Append('.');
            }

            if (!string.IsNullOrWhiteSpace(action.UsageExample))
            {
                line.Append(" Example: `").Append(action.UsageExample).Append("`.");
            }

            sb.AppendLine(line.ToString());
        }

        AppendSection(sb, "Outcomes");
        foreach (var outcome in Outcomes.All)
        {
            sb.Append("- **").Append(outcome.SerializedKind).Append("** (`").Append(outcome.Kind).Append("`) — ")
                .Append(outcome.Description)
                .Append(" Lead: ").Append(RenderToken(outcome.LeadingToken)).Append('.')
                .Append(" Argument: `").Append(outcome.ArgumentKind).Append("`.")
                .Append(" Example: `").Append(outcome.Example).AppendLine("`.");
        }

        AppendSection(sb, "Operators");
        foreach (var op in Operators.All)
        {
            sb.Append("- **").Append(RenderOperatorText(op)).Append("** (`").Append(op.Kind).Append("`) — ")
                .Append(op.Description)
                .Append(" Arity: `").Append(op.Arity).Append("`.")
                .Append(" Associativity: `").Append(op.Associativity).Append("`.")
                .Append(" Precedence: `").Append(op.Precedence).Append("`.")
                .Append(" Family: `").Append(op.Family).Append("`.")
                .Append(" Keyword operator: ").Append(RenderBool(op.IsKeywordOperator)).Append('.');

            if (!string.IsNullOrWhiteSpace(op.UsageExample))
            {
                sb.Append(" Example: `").Append(op.UsageExample).Append("`.");
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatTypes(string? scope = null)
    {
        if (!TryNormalizeTypeScope(scope, out var normalized))
        {
            return FormatInvalidScope(
                "Precept Type System",
                scope,
                ["types", "modifiers", "modifiers:value", "modifiers:state", "modifiers:event", "modifiers:access", "modifiers:anchor", "functions"]);
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Precept Type System");
        AppendScopeNote(
            sb,
            normalized,
            "Full output is large; prefer `types`, `modifiers`, `modifiers:value`, `modifiers:state`, `modifiers:event`, `modifiers:access`, `modifiers:anchor`, or `functions`.");

        if (normalized is null or "types")
        {
            AppendSection(sb, "Types");
            foreach (var type in Types.All)
            {
                var displayName = type.Token?.Text ?? type.DisplayName;
                var parts = new List<string>
                {
                    $"**{displayName}** — {type.Description}"
                };

                if (type.QualifierShape is not null)
                {
                    parts.Add($"Qualifiers: {RenderQualifierShape(type.QualifierShape)}");
                }

                if (HasFlags(type.Traits))
                {
                    parts.Add($"Traits: {JoinOrNone(ExpandFlags(type.Traits).Select(WrapCode))}");
                }

                if (type.WidensTo.Count > 0)
                {
                    parts.Add($"Widens to: {JoinOrNone(type.WidensTo.Select(RenderType))}");
                }

                if (type.ImpliedModifiers.Length > 0)
                {
                    parts.Add($"Implied modifiers: {JoinOrNone(type.ImpliedModifiers.Select(RenderModifier))}");
                }

                if (type.ImpliedQualifiers.Length > 0)
                {
                    parts.Add($"Implied qualifiers: {JoinOrNone(type.ImpliedQualifiers.Select(RenderImpliedQualifier))}");
                }

                if (type.Accessors.Count > 0)
                {
                    parts.Add($"Accessors: {JoinOrNone(type.Accessors.Select(RenderAccessor))}");
                }

                if (type.ChoiceLiteralTokens is { Count: > 0 })
                {
                    parts.Add($"Choice literals: {JoinOrNone(type.ChoiceLiteralTokens.Select(RenderToken))}");
                }

                if (type.ContentValidation is not null)
                {
                    parts.Add($"Validation: {RenderContentValidation(type.ContentValidation)}");
                }

                parts.Add($"notempty applies: {RenderBool(type.NotemptyApplicable)}");

                if (!string.IsNullOrWhiteSpace(type.UsageExample))
                {
                    parts.Add($"Example: `{type.UsageExample}`");
                }

                sb.Append("- ").Append(string.Join(". ", parts)).AppendLine(".");
            }
        }

        if (normalized is null || normalized.StartsWith("modifiers", StringComparison.Ordinal))
        {
            AppendSection(sb, "Modifiers");
            AppendModifierSection(sb, normalized, "Value Modifiers", Modifiers.All.OfType<ValueModifierMeta>(), modifier =>
            {
                var parts = new List<string>
                {
                    $"**{RenderModifier(modifier.Kind)}** (`{modifier.Kind}`) — {modifier.Description}",
                    $"Category: `{modifier.Category}`",
                    $"Applies to: {RenderTargets(modifier.ApplicableTo)}",
                    $"Declaration sites: {JoinOrNone(RenderDeclarationSites(modifier.ApplicableDeclarationSites))}",
                    $"Has value: {RenderBool(modifier.HasValue)}",
                    $"Subsumes: {JoinOrNone(modifier.Subsumes.Select(RenderModifier))}",
                    $"Mutually exclusive: {JoinOrNone(modifier.MutuallyExclusiveWith.Select(RenderModifier))}",
                    $"Desugars to rule: {RenderBool(modifier.DesugarsToRule)}"
                };

                if (modifier.ProofSatisfactions.Length > 0)
                {
                    parts.Add($"Proof satisfactions: {JoinOrNone(modifier.ProofSatisfactions.Select(RenderProofSatisfaction))}");
                }

                if (!string.IsNullOrWhiteSpace(modifier.UsageExample))
                {
                    parts.Add($"Example: `{modifier.UsageExample}`");
                }

                return string.Join(". ", parts) + ".";
            });

            AppendModifierSection(sb, normalized, "State Modifiers", Modifiers.All.OfType<StateModifierMeta>(), modifier =>
                $"**{RenderModifier(modifier.Kind)}** (`{modifier.Kind}`) — {modifier.Description}. Category: `{modifier.Category}`. Allows outgoing: {RenderBool(modifier.AllowsOutgoing)}. Requires dominator: {RenderBool(modifier.RequiresDominator)}. Prevents back-edge: {RenderBool(modifier.PreventsBackEdge)}. Mutually exclusive: {JoinOrNone(modifier.MutuallyExclusiveWith.Select(RenderModifier))}. Desugars to rule: {RenderBool(modifier.DesugarsToRule)}.");

            AppendModifierSection(sb, normalized, "Event Modifiers", Modifiers.All.OfType<EventModifierMeta>(), modifier =>
                $"**{RenderModifier(modifier.Kind)}** (`{modifier.Kind}`) — {modifier.Description}. Category: `{modifier.Category}`. Required analysis: `{modifier.RequiredAnalysis}`. Desugars to rule: {RenderBool(modifier.DesugarsToRule)}.");

            AppendModifierSection(sb, normalized, "Access Modifiers", Modifiers.All.OfType<AccessModifierMeta>(), modifier =>
                $"**{RenderModifier(modifier.Kind)}** (`{modifier.Kind}`) — {modifier.Description}. Category: `{modifier.Category}`. Present: {RenderBool(modifier.IsPresent)}. Writable: {RenderBool(modifier.IsWritable)}. Mutually exclusive: {JoinOrNone(modifier.MutuallyExclusiveWith.Select(RenderModifier))}. Desugars to rule: {RenderBool(modifier.DesugarsToRule)}.");

            AppendModifierSection(sb, normalized, "Anchor Modifiers", Modifiers.All.OfType<AnchorModifierMeta>(), modifier =>
                $"**{RenderModifier(modifier.Kind)}** (`{modifier.Kind}`) — {modifier.Description}. Category: `{modifier.Category}`. Scope: `{modifier.Scope}`. Target: `{modifier.Target}`. Desugars to rule: {RenderBool(modifier.DesugarsToRule)}.");
        }

        if (normalized is null or "functions")
        {
            AppendSection(sb, "Built-in Functions");
            foreach (var function in Functions.All)
            {
                var parts = new List<string>
                {
                    $"**{function.Name}** — {function.Description}",
                    $"Category: `{function.Category}`",
                    $"Signatures: {JoinOrNone(function.Overloads.Select(RenderFunctionOverload))}"
                };

                if (function.HasCIVariant)
                {
                    parts.Add($"Case-insensitive variant: `{function.CIVariantOf ?? function.Kind}` via `{function.CIDiagnosticCode}` diagnostics");
                }

                parts.Add($"Message position: {RenderBool(function.IsMessagePosition)}");

                if (!string.IsNullOrWhiteSpace(function.UsageExample))
                {
                    parts.Add($"Example: `{function.UsageExample}`");
                }

                sb.Append("- ").Append(string.Join(". ", parts)).AppendLine(".");
            }
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatOperations(string? category = null)
    {
        var filtered = string.IsNullOrWhiteSpace(category)
            ? Operations.All.ToArray()
            : Operations.All.Where(operation => string.Equals(operation switch
                {
                    UnaryOperationMeta unary => unary.Operand.Kind.ToString(),
                    BinaryOperationMeta binary => binary.Lhs.Kind.ToString(),
                    _ => string.Empty,
                }, category, StringComparison.OrdinalIgnoreCase)).ToArray();

        var categories = Operations.All
            .Select(operation => operation switch
            {
                UnaryOperationMeta unary => unary.Operand.Kind.ToString(),
                BinaryOperationMeta binary => binary.Lhs.Kind.ToString(),
                _ => string.Empty,
            })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var sb = new StringBuilder();
        sb.AppendLine("# Precept Operations");

        if (string.IsNullOrWhiteSpace(category))
        {
            sb.AppendLine("Prefer the `category` filter for routine calls; unfiltered output is the full operations catalog.");
        }
        else
        {
            sb.Append("Filtered by: `").Append(category.Trim()).AppendLine("`");
        }

        AppendSection(sb, "Available Categories");
        foreach (var item in categories)
        {
            AppendBullet(sb, WrapCode(item));
        }

        AppendSection(sb, "Matching Operations");
        if (filtered.Length == 0)
        {
            sb.AppendLine("None.");
        }
        else
        {
            foreach (var operation in filtered)
            {
                var signature = operation switch
                {
                    UnaryOperationMeta unary => $"{RenderOperatorText(Operators.GetMeta(unary.Op))} {RenderTypeName(unary.Operand.Kind)} -> {RenderTypeName(unary.Result)}",
                    BinaryOperationMeta binaryOperation => $"{RenderTypeName(binaryOperation.Lhs.Kind)} {RenderOperatorText(Operators.GetMeta(binaryOperation.Op))} {RenderTypeName(binaryOperation.Rhs.Kind)} -> {RenderTypeName(binaryOperation.Result)}",
                    _ => operation.Kind.ToString(),
                };

                var details = new List<string>
                {
                    $"**{operation.Kind}** — `{signature}`",
                    operation.Description
                };

                if (operation is BinaryOperationMeta binary)
                {
                    details.Add($"Qualifier match: `{binary.Match}`");
                    if (binary.ProofRequirements.Length > 0)
                    {
                        details.Add($"Proofs: {JoinOrNone(binary.ProofRequirements.Select(RenderProofRequirement))}");
                    }

                    if (binary.HasCIVariant && binary.CIDiagnosticCode is { } ciDiagnostic)
                    {
                        details.Add($"CI diagnostic: `{ciDiagnostic}`");
                    }

                    details.Add($"Bidirectional lookup: {RenderBool(binary.BidirectionalLookup)}");
                }
                else
                {
                    details.Add("Qualifier match: `Any`");
                }

                sb.Append("- ").Append(string.Join(". ", details)).AppendLine(".");
            }
        }

        AppendSection(sb, "Count");
        sb.Append(filtered.Length.ToString(CultureInfo.InvariantCulture));
        return sb.ToString().TrimEnd();
    }

    public static string FormatProofs()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Precept Proofs and Runtime Faults");

        AppendSection(sb, "Proof Requirements");
        foreach (var proof in ProofRequirements.All)
        {
            var dualSubject = proof is ProofRequirementMeta.QualifierCompatibility or ProofRequirementMeta.QualifierChain;
            sb.Append("- **").Append(proof.Kind).Append("** — ").Append(proof.Description)
                .Append(" Dual subject: ").Append(RenderBool(dualSubject)).AppendLine(".");
        }

        AppendSection(sb, "Runtime Faults");
        foreach (var fault in Faults.All)
        {
            sb.Append("- **").Append(fault.Code).Append("** — Severity: `").Append(fault.Severity)
                .Append("`. Message: ").Append(fault.MessageTemplate)
                .Append(" Recovery: ").Append(string.IsNullOrWhiteSpace(fault.RecoveryHint) ? "None." : fault.RecoveryHint)
                .AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatPatterns()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Precept Patterns");

        AppendSection(sb, "Common Patterns");
        foreach (var pattern in SyntaxReference.CommonPatterns)
        {
            sb.Append("### ").AppendLine(pattern.Name);
            sb.AppendLine(pattern.Description);
            AppendCodeBlock(sb, pattern.DslSnippet);
        }

        AppendSection(sb, "Anti-Patterns");
        foreach (var pattern in SyntaxReference.AntiPatterns)
        {
            sb.Append("### ").AppendLine(pattern.Name);
            sb.AppendLine(pattern.Description);
            sb.AppendLine("Bad:");
            AppendCodeBlock(sb, pattern.BadSnippet);
            sb.AppendLine("Good:");
            AppendCodeBlock(sb, pattern.GoodSnippet);
            sb.Append("Why it fails: ").AppendLine(pattern.WhyItFails);
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatDiagnostic(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return FormatDiagnosticNotFound(code);
        }

        var trimmed = code.Trim();
        var byName = Diagnostics.All.FirstOrDefault(diagnostic => string.Equals(diagnostic.Code, trimmed, StringComparison.OrdinalIgnoreCase));
        if (byName is not null)
        {
            return FormatDiagnosticEntry(byName);
        }

        if (trimmed.StartsWith("PRE", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(trimmed[3..], out var numericValue)
            && Enum.IsDefined(typeof(DiagnosticCode), numericValue))
        {
            return FormatDiagnosticEntry(Diagnostics.GetMeta((DiagnosticCode)numericValue));
        }

        return FormatDiagnosticNotFound(code);
    }

    public static string FormatDomains(string? scope = null)
    {
        if (!TryNormalizeDomainScope(scope, out var normalized))
        {
            return FormatInvalidScope(
                "Precept Domain Catalog",
                scope,
                ["currencies", "units", "prefixes", "dimensions", "temporal"]);
        }

        var currencies = CurrencyCatalog.All.Values.OrderBy(entry => entry.AlphaCode, StringComparer.Ordinal);
        var units = UcumAtomCatalog.BrowseTier1();
        var prefixes = UcumPrefixCatalog.All.Values.OrderBy(prefix => prefix.Order);
        var dimensions = DimensionCatalog.All.Values.OrderBy(entry => entry.Name, StringComparer.Ordinal);
        var temporalUnits = TemporalUnits.AllEntries;

        var sb = new StringBuilder();
        sb.AppendLine("# Precept Domain Catalog");
        AppendScopeNote(
            sb,
            normalized,
            "Full output is large; prefer `currencies`, `units`, `prefixes`, `dimensions`, or `temporal`.");

        if (normalized is null or "currencies")
        {
            AppendSection(sb, "Currencies");
            foreach (var currency in currencies)
            {
                sb.Append("- **").Append(currency.AlphaCode).Append("**");
                if (!string.IsNullOrWhiteSpace(currency.Symbol))
                {
                    sb.Append(" (`").Append(currency.Symbol).Append("`)");
                }

                sb.Append(" — ").Append(currency.Name)
                    .Append(". Numeric: `").Append(currency.NumericCode).Append("`.")
                    .Append(" Minor unit: `").Append(currency.MinorUnit).AppendLine("`.");
            }
        }

        if (normalized is null or "units")
        {
            AppendSection(sb, "UCUM Tier-1 Units");
            foreach (var unit in units)
            {
                var dimensionName = DimensionCatalog.TryGetAlias(unit.Vector, out var alias) && alias is not null
                    ? alias.Name
                    : null;

                sb.Append("- **").Append(unit.Code).Append("** — ").Append(unit.Name)
                    .Append(". Dimension: `").Append(RenderDimensionVector(unit.Vector)).Append("`.");

                if (!string.IsNullOrWhiteSpace(dimensionName))
                {
                    sb.Append(" Alias: `").Append(dimensionName).Append("`.");
                }

                sb.Append(" Scale: `").Append(RenderScale(unit.Scale)).Append("`.")
                    .Append(" Prefixable: ").Append(RenderBool(unit.Prefixable)).Append('.');

                if (!string.IsNullOrWhiteSpace(unit.AnnotationClass))
                {
                    sb.Append(" Annotation: `").Append(unit.AnnotationClass).Append("`.");
                }

                sb.AppendLine();
            }
        }

        if (normalized is null or "prefixes")
        {
            AppendSection(sb, "UCUM Prefixes");
            foreach (var prefix in prefixes)
            {
                sb.Append("- **").Append(prefix.Code).Append("** — ").Append(prefix.Name)
                    .Append(". Scale: `").Append(RenderScale(prefix.Factor)).Append("`.")
                    .Append(" Base-10 exponent: `").Append(prefix.Factor.Base10Exponent).AppendLine("`.");
            }
        }

        if (normalized is null or "dimensions")
        {
            AppendSection(sb, "Dimensions");
            foreach (var dimension in dimensions)
            {
                sb.Append("- **").Append(dimension.Name).Append("** — ").Append(dimension.Description)
                    .Append(". Vector: `").Append(RenderDimensionVector(dimension.Vector)).AppendLine("`.");
            }
        }

        if (normalized is null or "temporal")
        {
            AppendSection(sb, "Temporal Units");
            foreach (var entry in temporalUnits)
            {
                sb.Append("- **").Append(entry.Singular).Append(" / ").Append(entry.Plural).Append("** — ")
                    .Append("calendar-based: ").Append(RenderBool(entry.IsCalendarBased))
                    .Append("; period: ").Append(RenderBool(entry.IsPeriod))
                    .Append("; duration: ").Append(RenderBool(entry.IsDuration)).AppendLine(".");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatDiagnosticEntry(DiagnosticMeta diagnostic)
    {
        var sb = new StringBuilder();
        sb.Append("# Diagnostic ").Append(diagnostic.Code);
        if (TryFormatDiagnosticNumber(diagnostic.Code, out var number))
        {
            sb.Append(" (").Append(number).Append(')');
        }

        sb.AppendLine();
        AppendSection(sb, "Summary");
        AppendBullet(sb, $"Stage: `{diagnostic.Stage}`");
        AppendBullet(sb, $"Severity: `{diagnostic.Severity}`");
        AppendBullet(sb, $"Category: `{diagnostic.Category}`");
        AppendBullet(sb, $"Message: {diagnostic.MessageTemplate}");

        AppendSection(sb, "Trigger");
        sb.AppendLine(string.IsNullOrWhiteSpace(diagnostic.TriggerCondition) ? "None." : diagnostic.TriggerCondition);

        AppendSection(sb, "Recovery Steps");
        if (diagnostic.RecoverySteps is { Length: > 0 })
        {
            foreach (var step in diagnostic.RecoverySteps)
            {
                AppendBullet(sb, step);
            }
        }
        else
        {
            sb.AppendLine("None.");
        }

        AppendSection(sb, "Fix Hint");
        sb.AppendLine(string.IsNullOrWhiteSpace(diagnostic.FixHint) ? "None." : diagnostic.FixHint);

        AppendSection(sb, "Related Codes");
        sb.AppendLine(diagnostic.RelatedCodes is { Length: > 0 }
            ? JoinOrNone(diagnostic.RelatedCodes.Select(code => WrapCode(code.ToString())))
            : "None.");

        AppendSection(sb, "Prevents Fault");
        sb.AppendLine(diagnostic.PreventsFault?.ToString() is { Length: > 0 } preventsFault ? WrapCode(preventsFault) : "None.");

        if (!string.IsNullOrWhiteSpace(diagnostic.ExampleBefore) || !string.IsNullOrWhiteSpace(diagnostic.ExampleAfter))
        {
            AppendSection(sb, "Examples");
            if (!string.IsNullOrWhiteSpace(diagnostic.ExampleBefore))
            {
                sb.AppendLine("Before:");
                AppendCodeBlock(sb, diagnostic.ExampleBefore!);
            }

            if (!string.IsNullOrWhiteSpace(diagnostic.ExampleAfter))
            {
                sb.AppendLine("After:");
                AppendCodeBlock(sb, diagnostic.ExampleAfter!);
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatDiagnosticNotFound(string? code)
    {
        var requested = string.IsNullOrWhiteSpace(code) ? "<empty>" : code.Trim();
        return string.Join(Environment.NewLine,
        [
            "# Diagnostic Lookup Failed",
            $"Requested: `{requested}`",
            "Use a diagnostic code name such as `UndeclaredField` or a PRE number such as `PRE0017`."
        ]);
    }

    private static string FormatInvalidScope(string title, string? scope, IReadOnlyList<string> validScopes)
        => string.Join(Environment.NewLine,
        [
            $"# {title}",
            $"Unsupported scope: `{(string.IsNullOrWhiteSpace(scope) ? "<empty>" : scope.Trim())}`",
            $"Valid scopes: {JoinOrNone(validScopes.Select(WrapCode))}."
        ]);

    private static void AppendModifierSection<T>(StringBuilder sb, string? normalizedScope, string heading, IEnumerable<T> modifiers, Func<T, string> render)
        where T : ModifierMeta
    {
        if (!ShouldIncludeModifierSection(normalizedScope, heading))
        {
            return;
        }

        sb.Append("### ").AppendLine(heading);
        foreach (var modifier in modifiers)
        {
            AppendBullet(sb, render(modifier));
        }
    }

    private static bool ShouldIncludeModifierSection(string? normalizedScope, string heading)
        => normalizedScope is null or "modifiers"
            || (normalizedScope, heading) switch
            {
                ("modifiers:value", "Value Modifiers") => true,
                ("modifiers:state", "State Modifiers") => true,
                ("modifiers:event", "Event Modifiers") => true,
                ("modifiers:access", "Access Modifiers") => true,
                ("modifiers:anchor", "Anchor Modifiers") => true,
                _ => false,
            };

    private static void AppendScopeNote(StringBuilder sb, string? normalizedScope, string fullMessage)
    {
        if (normalizedScope is null)
        {
            sb.AppendLine(fullMessage);
        }
        else
        {
            sb.Append("Scope: `").Append(normalizedScope).AppendLine("`");
        }
    }

    private static void AppendSection(StringBuilder sb, string heading)
    {
        sb.AppendLine();
        sb.Append("## ").AppendLine(heading);
    }

    private static void AppendBullet(StringBuilder sb, string text)
        => sb.Append("- ").AppendLine(text);

    private static void AppendCodeBlock(StringBuilder sb, string code)
    {
        sb.AppendLine("```precept");
        sb.AppendLine(code.Trim());
        sb.AppendLine("```");
    }

    private static bool TryNormalizeTypeScope(string? scope, out string? normalized)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            normalized = null;
            return true;
        }

        normalized = scope.Trim().ToLowerInvariant();
        return normalized is "types" or "modifiers" or "modifiers:value" or "modifiers:state" or "modifiers:event" or "modifiers:access" or "modifiers:anchor" or "functions";
    }

    private static bool TryNormalizeDomainScope(string? scope, out string? normalized)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            normalized = null;
            return true;
        }

        normalized = scope.Trim().ToLowerInvariant();
        return normalized is "currencies" or "units" or "prefixes" or "dimensions" or "temporal";
    }

    private static string CollapseWhitespace(string text)
        => string.Join(' ', text.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string RenderType(TypeKind kind)
    {
        var meta = Types.GetMeta(kind);
        return WrapCode(meta.Token?.Text ?? meta.DisplayName);
    }

    private static string RenderModifier(ModifierKind kind)
        => WrapCode(Modifiers.GetMeta(kind).Token.Text ?? kind.ToString());

    private static string RenderToken(TokenKind kind)
        => WrapCode(Tokens.GetMeta(kind).Text ?? kind.ToString());

    private static string RenderConstructScopes(IEnumerable<ConstructKind> kinds)
    {
        var values = kinds.Select(kind => WrapCode(kind.ToString())).ToArray();
        return values.Length == 0 ? "top-level" : string.Join(", ", values);
    }

    private static string RenderConstructSlot(ConstructSlot slot)
    {
        var description = slot.Description is { Length: > 0 } ? $": {slot.Description}" : string.Empty;
        var termination = slot.TerminationTokens?.Length > 0
            ? $"; stops at {JoinOrNone(slot.TerminationTokens.Select(RenderToken))}"
            : string.Empty;
        return $"`{slot.Kind}` ({(slot.IsRequired ? "required" : "optional")}{description}{termination})";
    }

    private static string RenderDisambiguationEntry(DisambiguationEntry entry)
    {
        var disambiguationTokens = entry.DisambiguationTokens.HasValue
            ? entry.DisambiguationTokens.Value
            : [];
        var tokens = disambiguationTokens.Length > 0
            ? JoinOrNone(disambiguationTokens.Select(token => RenderToken(token)))
            : "None.";
        return $"{RenderToken(entry.LeadingToken)} -> {tokens}";
    }

    private static string RenderTargets(TypeTarget[] targets)
    {
        if (targets.Length == 0)
        {
            return "any type";
        }

        return string.Join(", ", targets.Select(RenderTarget));
    }

    private static string RenderTarget(TypeTarget target)
        => target switch
        {
            ModifiedTypeTarget modified => modified.Kind is null
                ? $"any type with {JoinOrNone(modified.RequiredModifiers.Select(RenderModifier))}"
                : $"{RenderType(modified.Kind.Value)} with {JoinOrNone(modified.RequiredModifiers.Select(RenderModifier))}",
            { Kind: null } => "any type",
            _ => RenderType(target.Kind!.Value),
        };

    private static IEnumerable<string> RenderDeclarationSites(ValueModifierDeclarationSite declarationSites)
        => Enum.GetValues<ValueModifierDeclarationSite>()
            .Where(site => site is not ValueModifierDeclarationSite.None && declarationSites.HasFlag(site))
            .Select(site => WrapCode(site.ToString()));

    private static string RenderQualifierShape(QualifierShape shape)
        => JoinOrNone(shape.Slots.Select(slot => $"{RenderToken(slot.Preposition)} {RenderSlotAxisLabel(slot.Axis)}"))
            + (shape.InOfExclusive ? "; `in`/`of` are mutually exclusive" : string.Empty)
            + (shape.OfRequiresCurrencyIn ? "; `of` requires `in` to resolve to a currency" : string.Empty);

    private static string RenderSlotAxisLabel(QualifierAxis axis) => axis switch
    {
        QualifierAxis.PriceIn => "`currency`, `unit`, or compound `currency/unit`",
        _ => $"`{axis}`",
    };

    private static string RenderAccessor(TypeAccessor accessor)
        => accessor switch
        {
            FixedReturnAccessor fixedReturn => $"`{fixedReturn.Name}` -> {RenderType(fixedReturn.Returns)}{RenderAccessorExtras(fixedReturn)}",
            ElementParameterAccessor elementParameter => $"`{elementParameter.Name}` -> element (parameter: element){RenderAccessorExtras(elementParameter)}",
            _ => $"`{accessor.Name}` -> element{RenderAccessorExtras(accessor)}",
        };

    private static string RenderAccessorExtras(TypeAccessor accessor)
    {
        var extras = new List<string>();
        if (accessor.ParameterType is { } parameterType)
        {
            extras.Add($"parameter {RenderType(parameterType)}");
        }

        if (HasFlags(accessor.RequiredTraits))
        {
            extras.Add($"requires {JoinOrNone(ExpandFlags(accessor.RequiredTraits).Select(WrapCode))}");
        }

        if (accessor.ProofRequirements.Length > 0)
        {
            extras.Add($"proofs {JoinOrNone(accessor.ProofRequirements.Select(RenderProofRequirement))}");
        }

        return extras.Count == 0 ? string.Empty : $" ({string.Join("; ", extras)})";
    }

    private static string RenderContentValidation(ContentValidation validation)
        => validation switch
        {
            ClosedSetValidation closedSet => $"{closedSet.SetName}; examples: {JoinOrNone(closedSet.Examples.Select(WrapCode))}",
            NodaTimeValidation nodaTime => $"{nodaTime.FormatDescription}; pattern `{nodaTime.NodaTimePattern}`",
            RegexValidation regex => regex.FormatDescription,
            UcumValidation ucum => ucum.FormatDescription,
            MoneyValidation money => money.FormatDescription,
            QuantityValidation quantity => quantity.FormatDescription,
            PriceValidation price => price.FormatDescription,
            ExchangeRateValidation exchangeRate => exchangeRate.FormatDescription,
            _ => validation.FormatDescription,
        };

    private static string RenderFunctionOverload(FunctionOverload overload)
    {
        var parameters = overload.Parameters.Select(parameter => parameter.Name is { Length: > 0 }
            ? $"{parameter.Name}: {RenderTypeName(parameter.Kind)}"
            : RenderTypeName(parameter.Kind));

        var signature = $"{string.Join(", ", parameters)} -> {RenderTypeName(overload.ReturnType)}";
        var extras = new List<string>();

        if (overload.Match is { } match)
        {
            extras.Add($"match `{match}`");
        }

        if (overload.ProofRequirements.Length > 0)
        {
            extras.Add($"proofs {JoinOrNone(overload.ProofRequirements.Select(RenderProofRequirement))}");
        }

        return extras.Count == 0 ? WrapCode(signature) : $"{WrapCode(signature)} ({string.Join("; ", extras)})";
    }

    private static string RenderProofRequirement(ProofRequirement proofRequirement)
        => proofRequirement switch
        {
            NumericProofRequirement numeric => $"{RenderProofSubject(numeric.Subject)} {RenderComparison(numeric.Comparison)} {numeric.Threshold.ToString(CultureInfo.InvariantCulture)} — {numeric.Description}",
            PresenceProofRequirement presence => $"{RenderProofSubject(presence.Subject)} is present — {presence.Description}",
            DimensionProofRequirement dimension => $"{RenderProofSubject(dimension.Subject)} has {dimension.RequiredDimension.ToString().ToLowerInvariant()} temporal dimension — {dimension.Description}",
            QualifierCompatibilityProofRequirement qualifierCompatibility => $"{RenderCompatibleSubjects(qualifierCompatibility.LeftSubject, qualifierCompatibility.RightSubject)} share {qualifierCompatibility.Axis.ToString().ToLowerInvariant()} qualifiers — {qualifierCompatibility.Description}",
            QualifierChainProofRequirement chain => $"{RenderCompatibleSubjects(chain.LeftSubject, chain.RightSubject)} chain {chain.LeftAxis.ToString().ToLowerInvariant()}↔{chain.RightAxis.ToString().ToLowerInvariant()} — {chain.Description}",
            ModifierRequirement modifier => $"{RenderProofSubject(modifier.Subject)} declares {RenderModifier(modifier.Required)} — {modifier.Description}",
            _ => throw new ArgumentOutOfRangeException(nameof(proofRequirement), proofRequirement, null),
        };

    private static string RenderProofSatisfaction(ProofSatisfaction proofSatisfaction)
        => proofSatisfaction switch
        {
            ProofSatisfaction.Numeric numeric => $"{RenderProjection(numeric.Projection)} {RenderComparison(numeric.Comparison)} {RenderBoundSource(numeric.Bound)}",
            ProofSatisfaction.Presence => "self is present",
            ProofSatisfaction.Dimension dimension => $"self has {RenderDimensionSource(dimension.Source)} temporal dimension",
            ProofSatisfaction.Modifier modifier => $"self declares {RenderModifier(modifier.RequiredModifier)}",
            ProofSatisfaction.QualifierCompatibility qualifierCompatibility => $"operands share {qualifierCompatibility.Axis.ToString().ToLowerInvariant()} qualifiers",
            _ => throw new ArgumentOutOfRangeException(nameof(proofSatisfaction), proofSatisfaction, null),
        };

    private static string RenderProjection(SatisfactionProjection projection)
        => projection switch
        {
            SatisfactionProjection.SelfValue => "self",
            SatisfactionProjection.Accessor accessor => $"self.{accessor.Name}",
            _ => throw new ArgumentOutOfRangeException(nameof(projection), projection, null),
        };

    private static string RenderBoundSource(NumericBoundSource bound)
        => bound switch
        {
            NumericBoundSource.Constant constant => constant.Value.ToString(CultureInfo.InvariantCulture),
            NumericBoundSource.DeclarationValue => "declaration value",
            _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, null),
        };

    private static string RenderDimensionSource(DimensionSource source)
        => source switch
        {
            DimensionSource.Constant constant => constant.Value.ToString().ToLowerInvariant(),
            DimensionSource.DeclaredTemporalDimension => "declared",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };

    private static string RenderCompatibleSubjects(ProofSubject left, ProofSubject right)
    {
        var leftText = RenderProofSubject(left);
        var rightText = RenderProofSubject(right);
        return leftText == rightText ? "operands" : $"{leftText} and {rightText}";
    }

    private static string RenderProofSubject(ProofSubject subject)
        => subject switch
        {
            ParamSubject parameter => parameter.Parameter.Name ?? parameter.Parameter.Kind.ToString().ToLowerInvariant(),
            SelfSubject self => self.Accessor is null ? "self" : $"self.{self.Accessor.Name}",
            _ => throw new ArgumentOutOfRangeException(nameof(subject), subject, null),
        };

    private static string RenderComparison(OperatorKind comparison)
        => comparison switch
        {
            OperatorKind.Equals => "==",
            OperatorKind.NotEquals => "!=",
            OperatorKind.LessThan => "<",
            OperatorKind.GreaterThan => ">",
            OperatorKind.LessThanOrEqual => "<=",
            OperatorKind.GreaterThanOrEqual => ">=",
            _ => comparison.ToString(),
        };

    private static string RenderOperatorText(OperatorMeta op)
        => op switch
        {
            SingleTokenOp single => single.Token.Text ?? single.Kind.ToString(),
            MultiTokenOp multi => string.Join(' ', multi.Tokens.Select(token => token.Text ?? token.Kind.ToString())),
            _ => op.Kind.ToString(),
        };

    private static string RenderImpliedQualifier(DeclaredQualifierMeta qualifier) => qualifier switch
    {
        DeclaredQualifierMeta.TemporalDimension temporalDimension => WrapCode($"TemporalDimension:{temporalDimension.Value}"),
        DeclaredQualifierMeta.TemporalUnit temporalUnit => WrapCode($"TemporalUnit:{temporalUnit.UnitName}"),
        DeclaredQualifierMeta.Dimension dimension => WrapCode($"Dimension:{dimension.DimensionName}"),
        DeclaredQualifierMeta.Currency currency => WrapCode($"Currency:{currency.CurrencyCode}"),
        DeclaredQualifierMeta.Unit unit => WrapCode($"Unit:{unit.UnitCode}"),
        DeclaredQualifierMeta.FromCurrency fromCurrency => WrapCode($"FromCurrency:{fromCurrency.CurrencyCode}"),
        DeclaredQualifierMeta.ToCurrency toCurrency => WrapCode($"ToCurrency:{toCurrency.CurrencyCode}"),
        DeclaredQualifierMeta.Timezone timezone => WrapCode($"Timezone:{timezone.TimezoneId}"),
        DeclaredQualifierMeta.CompoundPrice compound => WrapCode($"CompoundPrice:{compound.CurrencyCode}/{compound.UnitCode} ({compound.DimensionName})"),
        _ => WrapCode(qualifier.Axis.ToString()),
    };

    private static string RenderDimensionVector(DimensionVector vector)
        => $"L{vector.Length} M{vector.Mass} T{vector.Time} I{vector.ElectricCurrent} Θ{vector.Temperature} N{vector.AmountOfSubstance} J{vector.LuminousIntensity}";

    private static string RenderScale(UcumExactFactor factor)
        => $"{factor.Numerator}/{factor.Denominator} × 10^{factor.Base10Exponent}";

    private static bool TryFormatDiagnosticNumber(string code, out string number)
    {
        if (Enum.TryParse<DiagnosticCode>(code, out var diagnosticCode))
        {
            number = $"PRE{(int)diagnosticCode:D4}";
            return true;
        }

        number = string.Empty;
        return false;
    }

    private static string RenderTypeName(TypeKind kind)
    {
        var meta = Types.GetMeta(kind);
        return meta.Token?.Text ?? meta.DisplayName;
    }

    private static bool HasFlags<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0;

    private static IEnumerable<string> ExpandFlags<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => Enum.GetValues<TEnum>()
            .Where(flag => Convert.ToInt64(flag, CultureInfo.InvariantCulture) != 0 && value.HasFlag(flag))
            .Select(flag => flag.ToString());

    private static string JoinOrNone(IEnumerable<string> values)
    {
        var items = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return items.Length == 0 ? "None" : string.Join(", ", items);
    }

    private static string RenderBool(bool value) => value ? "yes" : "no";

    private static string WrapCode(string value) => $"`{value}`";
}
