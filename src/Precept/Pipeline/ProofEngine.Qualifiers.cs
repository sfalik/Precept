using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public static partial class ProofEngine
{
    // ── Strategy 5: Qualifier Compatibility Proof ─────────────────────────────

    private static bool TryQualifierCompatibilityProof(ProofObligation obligation, SemanticIndex semantics)
    {
        if (obligation.Requirement is QualifierCompatibilityProofRequirement qcReq)
        {
            if (obligation.Site is not TypedBinaryOp binOp)
                return false;

            var leftQualifier = ResolveQualifierFromExpression(binOp.Left, qcReq.Axis, semantics);
            var rightQualifier = ResolveQualifierFromExpression(binOp.Right, qcReq.Axis, semantics);

            return QualifiersAreCompatible(leftQualifier, rightQualifier, qcReq.Axis);
        }

        if (obligation.Requirement is QualifierChainProofRequirement chainReq)
        {
            var leftQualifier = ResolveQualifierOnAxis(chainReq.LeftSubject, chainReq.LeftAxis, obligation.Site, semantics);
            var rightQualifier = ResolveQualifierOnAxis(chainReq.RightSubject, chainReq.RightAxis, obligation.Site, semantics);

            if (leftQualifier is null || rightQualifier is null)
                return false;

            return ChainQualifiersMatch(leftQualifier, rightQualifier);
        }

        return false;
    }

    /// <summary>
    /// Compares two qualifier values across potentially different axes by extracting
    /// their comparable string values.
    /// </summary>
    private static bool ChainQualifiersMatch(DeclaredQualifierMeta left, DeclaredQualifierMeta right)
    {
        var leftValue = ExtractComparableValue(left);
        var rightValue = ExtractComparableValue(right);
        if (leftValue is not null && rightValue is not null
            && string.Equals(leftValue, rightValue, StringComparison.Ordinal))
        {
            return true;
        }

        return QualifiersSymbolicallyEqual(left, right);
    }

    private static bool QualifiersAreCompatible(
        DeclaredQualifierMeta? leftQualifier,
        DeclaredQualifierMeta? rightQualifier,
        QualifierAxis axis)
    {
        if (leftQualifier is null || rightQualifier is null)
            return false;

        if (axis == QualifierAxis.TemporalDimension)
        {
            if (leftQualifier is DeclaredQualifierMeta.TemporalDimension { Value: PeriodDimension.Any }
                || rightQualifier is DeclaredQualifierMeta.TemporalDimension { Value: PeriodDimension.Any })
            {
                return false;
            }
        }

        if (leftQualifier == rightQualifier || QualifiersSymbolicallyEqual(leftQualifier, rightQualifier))
            return true;

        // Cross-type dimension compatibility: Unit(code, dim) and Dimension(dim) denote the same
        // dimension at different levels of specificity (e.g., a dimension-only price field vs a
        // unit-qualified typed constant). Consider them compatible if their dimension names agree.
        if (axis == QualifierAxis.Unit || axis == QualifierAxis.Dimension)
        {
            string? leftDim = leftQualifier switch
            {
                DeclaredQualifierMeta.Unit { DimensionName: var d } => d,
                DeclaredQualifierMeta.Dimension { DimensionName: var d } => d,
                _ => null,
            };
            string? rightDim = rightQualifier switch
            {
                DeclaredQualifierMeta.Unit { DimensionName: var d } => d,
                DeclaredQualifierMeta.Dimension { DimensionName: var d } => d,
                _ => null,
            };
            if (leftDim is not null && rightDim is not null
                && !string.IsNullOrEmpty(leftDim)
                && string.Equals(leftDim, rightDim, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // CompoundPrice compatibility: two CompoundPrice qualifiers are compatible if
        // both CurrencyCode and UnitCode match. CompoundPrice vs any other subtype is incompatible.
        if (leftQualifier is DeclaredQualifierMeta.CompoundPrice leftCompound
            && rightQualifier is DeclaredQualifierMeta.CompoundPrice rightCompound)
        {
            return string.Equals(leftCompound.CurrencyCode, rightCompound.CurrencyCode, StringComparison.Ordinal)
                && string.Equals(leftCompound.UnitCode, rightCompound.UnitCode, StringComparison.Ordinal);
        }

        return false;
    }

    private static bool QualifiersSymbolicallyEqual(DeclaredQualifierMeta left, DeclaredQualifierMeta right)
    {
        // Primary: SourceFieldName populated at type-check time — cross-subtype capable
        if (left.SourceFieldName is { } lsf && right.SourceFieldName is { } rsf)
            return string.Equals(lsf, rsf, StringComparison.Ordinal);

        // Fallback: structural path extraction for legacy/non-interpolated qualifiers
        var leftSourcePath = ExtractQualifierSourcePath(left);
        var rightSourcePath = ExtractQualifierSourcePath(right);
        return leftSourcePath is not null
            && rightSourcePath is not null
            && string.Equals(leftSourcePath, rightSourcePath, StringComparison.Ordinal);
    }

    private static string? ExtractQualifierSourcePath(DeclaredQualifierMeta qualifier)
    {
        var raw = qualifier switch
        {
            DeclaredQualifierMeta.Currency { CurrencyCode: var value } => value,
            DeclaredQualifierMeta.FromCurrency { CurrencyCode: var value } => value,
            DeclaredQualifierMeta.ToCurrency { CurrencyCode: var value } => value,
            DeclaredQualifierMeta.Unit { UnitCode: var value } => value,
            DeclaredQualifierMeta.Dimension { DimensionName: var value } => value,
            DeclaredQualifierMeta.TemporalUnit { UnitName: var value } => value,
            // TODO: When interpolated CompoundPrice is implemented, this must extract both
            // CurrencyCode and UnitCode as a composite source path (e.g. "CurrencyCode/UnitCode")
            // to correctly distinguish qualifiers that share a currency but differ in unit.
            // Safe today because no InterpolationSlotKind.PriceIn exists yet.
            DeclaredQualifierMeta.CompoundPrice { CurrencyCode: var value } => value,
            _ => null,
        };

        if (string.IsNullOrEmpty(raw)
            || raw[0] != '{'
            || raw[^1] != '}')
        {
            return null;
        }

        var inner = raw[1..^1];
        var dotIndex = inner.IndexOf('.');
        return dotIndex >= 0 ? inner[..dotIndex] : inner;
    }

    /// <summary>
    /// Extracts a string value suitable for cross-axis comparison from a qualifier.
    /// </summary>
    private static string? ExtractComparableValue(DeclaredQualifierMeta qualifier) => qualifier switch
    {
        DeclaredQualifierMeta.Currency c       => c.CurrencyCode,
        DeclaredQualifierMeta.FromCurrency fc  => fc.CurrencyCode,
        DeclaredQualifierMeta.ToCurrency tc    => tc.CurrencyCode,
        DeclaredQualifierMeta.Unit u           => u.UnitCode,
        DeclaredQualifierMeta.Dimension d      => d.DimensionName,
        DeclaredQualifierMeta.TemporalUnit tu  => tu.UnitName,
        DeclaredQualifierMeta.TemporalDimension td => td.Value switch
        {
            PeriodDimension.Date => "date",
            PeriodDimension.Time => "time",
            _                    => null,   // PeriodDimension.Any cannot satisfy chain comparisons
        },
        DeclaredQualifierMeta.CompoundPrice cp => $"{cp.CurrencyCode}/{cp.UnitCode}",
        _                                      => null,
    };

    private static DeclaredQualifierMeta? ResolveQualifierOnAxis(
        ProofSubject subject, QualifierAxis axis, TypedExpression site, SemanticIndex semantics)
    {
        var resolved = ResolveSubject(subject, site);
        if (resolved is TypedArgRef { DeclaredQualifiers: { } argQualifiers })
        {
            foreach (var qual in argQualifiers)
            {
                if (qual.Axis == axis)
                    return qual;
            }

            if (axis == QualifierAxis.Unit)
            {
                foreach (var qual in argQualifiers)
                {
                    if (qual.Axis == QualifierAxis.Dimension)
                        return qual;
                }
            }

            if (axis == QualifierAxis.Dimension)
            {
                foreach (var qual in argQualifiers)
                {
                    if (qual.Axis == QualifierAxis.TemporalDimension)
                        return qual;
                }
            }

            // PriceIn fallback: project CompoundPrice onto Currency/Unit/Dimension axes
            foreach (var qual in argQualifiers)
            {
                var projected = TryProjectCompoundPrice(qual, axis);
                if (projected is not null) return projected;
            }
        }

        if (resolved is TypedTypedConstant { DeclaredQualifiers: { } tcQualifiers })
        {
            foreach (var qual in tcQualifiers)
            {
                if (qual.Axis == axis)
                    return qual;
            }

            if (axis == QualifierAxis.Unit)
            {
                foreach (var qual in tcQualifiers)
                {
                    if (qual.Axis == QualifierAxis.Dimension)
                        return qual;
                }
            }

            if (axis == QualifierAxis.Dimension)
            {
                foreach (var qual in tcQualifiers)
                {
                    if (qual.Axis == QualifierAxis.TemporalDimension)
                        return qual;
                }
            }

            // PriceIn fallback: project CompoundPrice onto Currency/Unit/Dimension axes
            foreach (var qual in tcQualifiers)
            {
                var projected = TryProjectCompoundPrice(qual, axis);
                if (projected is not null) return projected;
            }
        }

        // Transitive qualifier resolution through binary operations:
        // When the resolved subject is a TypedBinaryOp (a subexpression), its
        // ResultQualifier tells us how to derive the result's qualifiers.
        if (resolved is TypedBinaryOp binOp && binOp.ResultQualifier is not null)
        {
            switch (binOp.ResultQualifier)
            {
                case SameQualifierRequired:
                    return ResolveQualifierFromExpression(binOp.Left, axis, semantics);

                case QualifiedOperandInherited:
                    var qualifiedOperand = binOp.Left.ResultType == binOp.ResultType
                        ? binOp.Left : binOp.Right;
                    return ResolveQualifierFromExpression(qualifiedOperand, axis, semantics);

                case CompoundUnitCancellationRequired:
                    if (axis == QualifierAxis.Currency
                        || axis == QualifierAxis.FromCurrency
                        || axis == QualifierAxis.ToCurrency)
                    {
                        return ResolveQualifierFromExpression(binOp.Left, axis, semantics)
                            ?? ResolveQualifierFromExpression(binOp.Right, axis, semantics);
                    }

                    return TryResolveCompoundCancellationUnit(binOp, axis, semantics);

                case CurrencyConversionRequired:
                    if (axis == QualifierAxis.Currency)
                    {
                        // Result currency = exchangerate operand's ToCurrency
                        return ResolveQualifierFromExpression(
                            binOp.Left.ResultType == TypeKind.ExchangeRate ? binOp.Left : binOp.Right,
                            QualifierAxis.ToCurrency, semantics);
                    }
                    return null;

                case CompoundDimensionElevationRequired:
                    if (axis == QualifierAxis.Currency
                        || axis == QualifierAxis.FromCurrency
                        || axis == QualifierAxis.ToCurrency)
                    {
                        // Currency inherits from price (left operand)
                        return ResolveQualifierFromExpression(binOp.Left, axis, semantics);
                    }
                    // Unit/Dimension: elevated from compound-quantity numerator (right operand)
                    return TryResolveCompoundElevationDimension(binOp, axis, semantics);
            }
        }

        var fieldName = GetFieldName(resolved);
        if (fieldName is null) return null;
        if (!semantics.FieldsByName.TryGetValue(fieldName, out var field)) return null;

        foreach (var qual in field.DeclaredQualifiers)
        {
            if (qual.Axis == axis)
                return qual;
        }

        // Axis fallback: Unit → Dimension (dimension-only fields satisfy unit-axis proofs)
        if (axis == QualifierAxis.Unit)
        {
            foreach (var qual in field.DeclaredQualifiers)
            {
                if (qual.Axis == QualifierAxis.Dimension)
                    return qual;
            }
        }

        // Axis fallback: Dimension → TemporalDimension (price of 'time'/'date' satisfies Dimension-axis proofs)
        if (axis == QualifierAxis.Dimension)
        {
            foreach (var qual in field.DeclaredQualifiers)
            {
                if (qual.Axis == QualifierAxis.TemporalDimension)
                    return qual;
            }
        }

        // PriceIn fallback: project CompoundPrice onto Currency/Unit/Dimension axes
        foreach (var qual in field.DeclaredQualifiers)
        {
            var projected = TryProjectCompoundPrice(qual, axis);
            if (projected is not null) return projected;
        }

        // Implied qualifiers: check type-level metadata after declared qualifiers are exhausted
        // (e.g., duration carries implied TemporalDimension(Time, Baseline))
        var typeMeta = Types.GetMeta(field.ResolvedType);
        foreach (var qual in typeMeta.ImpliedQualifiers)
        {
            if (qual.Axis == axis)
                return qual;
        }

        return null;
    }

    /// <summary>
    /// Resolve a qualifier on an axis from an arbitrary typed expression.
    /// Handles field refs, arg refs, and recursive binary ops.
    /// </summary>
    private static DeclaredQualifierMeta? ResolveQualifierFromExpression(
        TypedExpression expr, QualifierAxis axis, SemanticIndex semantics)
    {
        switch (expr)
        {
            case TypedArgRef { DeclaredQualifiers: { IsDefaultOrEmpty: false } argQuals }:
                foreach (var q in argQuals)
                    if (q.Axis == axis) return q;
                if (axis == QualifierAxis.Unit)
                    foreach (var q in argQuals)
                        if (q.Axis == QualifierAxis.Dimension) return q;
                if (axis == QualifierAxis.Dimension)
                    foreach (var q in argQuals)
                        if (q.Axis == QualifierAxis.TemporalDimension) return q;
                // PriceIn fallback: project CompoundPrice onto Currency/Unit/Dimension axes
                foreach (var q in argQuals)
                {
                    var projected = TryProjectCompoundPrice(q, axis);
                    if (projected is not null) return projected;
                }
                return null;

            case TypedTypedConstant { DeclaredQualifiers: { IsDefaultOrEmpty: false } tcQuals }:
                foreach (var q in tcQuals)
                    if (q.Axis == axis) return q;
                if (axis == QualifierAxis.Unit)
                    foreach (var q in tcQuals)
                        if (q.Axis == QualifierAxis.Dimension) return q;
                if (axis == QualifierAxis.Dimension)
                    foreach (var q in tcQuals)
                        if (q.Axis == QualifierAxis.TemporalDimension) return q;
                // PriceIn fallback: project CompoundPrice onto Currency/Unit/Dimension axes
                foreach (var q in tcQuals)
                {
                    var projected = TryProjectCompoundPrice(q, axis);
                    if (projected is not null) return projected;
                }
                return null;

            case TypedFieldRef fieldRef:
                return ResolveFieldQualifier(fieldRef.FieldName, axis, semantics);

            case TypedMemberAccess { Object: TypedFieldRef fieldRef2 }:
                return ResolveFieldQualifier(fieldRef2.FieldName, axis, semantics);

            case InterpolatedTypedConstant itc:
                return ResolveQualifierFromInterpolatedConstant(itc, axis);

            case TypedBinaryOp binOp when binOp.ResultQualifier is not null:
                return binOp.ResultQualifier switch
                {
                    SameQualifierRequired =>
                        ResolveQualifierFromExpression(binOp.Left, axis, semantics),
                    QualifiedOperandInherited =>
                        ResolveQualifierFromExpression(
                            binOp.Left.ResultType == binOp.ResultType ? binOp.Left : binOp.Right,
                            axis, semantics),
                    CompoundUnitCancellationRequired =>
                        axis == QualifierAxis.Currency
                        || axis == QualifierAxis.FromCurrency
                        || axis == QualifierAxis.ToCurrency
                            ? ResolveQualifierFromExpression(binOp.Left, axis, semantics)
                                ?? ResolveQualifierFromExpression(binOp.Right, axis, semantics)
                            : TryResolveCompoundCancellationUnit(binOp, axis, semantics),
                    CurrencyConversionRequired =>
                        axis == QualifierAxis.Currency
                            // Result currency = exchangerate operand's ToCurrency.
                            // Translate the exchange-rate axis back onto the caller's Currency axis
                            // so qualifier compatibility compares like-for-like in nested expressions.
                            ? TranslateCurrencyAxis(
                                ResolveQualifierFromExpression(
                                    binOp.Left.ResultType == TypeKind.ExchangeRate ? binOp.Left : binOp.Right,
                                    QualifierAxis.ToCurrency, semantics))
                            : null,
                    CompoundDimensionElevationRequired =>
                        axis == QualifierAxis.Currency
                        || axis == QualifierAxis.FromCurrency
                        || axis == QualifierAxis.ToCurrency
                            // Currency inherits from price (left operand)
                            ? ResolveQualifierFromExpression(binOp.Left, axis, semantics)
                            // Unit/Dimension: elevated from compound-quantity numerator
                            : TryResolveCompoundElevationDimension(binOp, axis, semantics),
                    _ => null,
                };

            default:
                return null;
        }
    }

    private static DeclaredQualifierMeta? TranslateCurrencyAxis(DeclaredQualifierMeta? qualifier)
    {
        return qualifier switch
        {
            DeclaredQualifierMeta.ToCurrency toCurrency => new DeclaredQualifierMeta.Currency(
                toCurrency.CurrencyCode,
                toCurrency.Origin,
                TokenKind.In,
                toCurrency.ProofSatisfactions,
                toCurrency.SourceFieldName),
            _ => qualifier,
        };
    }

    private static DeclaredQualifierMeta? ResolveQualifierFromInterpolatedConstant(
        InterpolatedTypedConstant itc, QualifierAxis axis)
    {
        // If StaticQualifier was resolved at type-check time, trust it directly.
        // ResolveStaticQualifier only sets a value when no dynamic slot covers the same axis,
        // so early-returning here can never shadow a runtime-field-sourced qualifier.
        if (itc.StaticQualifier is { } staticQual)
        {
            var resolved = (staticQual, axis) switch
            {
                (StaticCurrencyQualifier { CurrencyCode: var code }, QualifierAxis.Currency) =>
                    (DeclaredQualifierMeta?)new DeclaredQualifierMeta.Currency(code),
                (StaticUnitQualifier { Unit: var unit }, QualifierAxis.Unit) =>
                    new DeclaredQualifierMeta.Unit(unit.CanonicalCode, UnitDimensionHelper.DeriveUnitDimensionName(unit)),
                (StaticUnitQualifier { Unit: var unit }, QualifierAxis.Dimension) =>
                    new DeclaredQualifierMeta.Dimension(UnitDimensionHelper.DeriveUnitDimensionName(unit)),
                (StaticCurrencyAndUnitQualifier { CurrencyCode: var code }, QualifierAxis.Currency) =>
                    new DeclaredQualifierMeta.Currency(code),
                (StaticCurrencyAndUnitQualifier { Unit: var unit }, QualifierAxis.Unit) =>
                    new DeclaredQualifierMeta.Unit(unit.CanonicalCode, UnitDimensionHelper.DeriveUnitDimensionName(unit)),
                (StaticCurrencyAndUnitQualifier { Unit: var unit }, QualifierAxis.Dimension) =>
                    new DeclaredQualifierMeta.Dimension(UnitDimensionHelper.DeriveUnitDimensionName(unit)),
                (StaticFromToCurrenciesQualifier { FromCode: var fromCode }, QualifierAxis.FromCurrency) =>
                    new DeclaredQualifierMeta.FromCurrency(fromCode),
                (StaticFromToCurrenciesQualifier { ToCode: var toCode }, QualifierAxis.ToCurrency) =>
                    new DeclaredQualifierMeta.ToCurrency(toCode),
                _ => null,
            };
            if (resolved is not null)
                return resolved;
            // StaticQualifier exists but doesn't cover this axis — fall through to slot logic.
        }

        InterpolationSlotKind? targetSlot = axis switch
        {
            QualifierAxis.Currency => InterpolationSlotKind.Currency,
            QualifierAxis.Unit => InterpolationSlotKind.Unit,
            QualifierAxis.Dimension => InterpolationSlotKind.Unit,
            QualifierAxis.FromCurrency => InterpolationSlotKind.FromCurrency,
            QualifierAxis.ToCurrency => InterpolationSlotKind.ToCurrency,
            _ => null,
        };

        if (targetSlot is null)
            return null;

        foreach (var slot in itc.Slots)
        {
            if (slot.SlotKind == targetSlot)
                return CreateQualifierFromSlotExpression(slot.Expression, axis);
        }

        if (axis == QualifierAxis.Currency)
        {
            foreach (var slot in itc.Slots)
            {
                if (slot.SlotKind == InterpolationSlotKind.NumeratorUnit)
                    return CreateQualifierFromSlotExpression(slot.Expression, axis);
            }
        }

        if (axis == QualifierAxis.Unit || axis == QualifierAxis.Dimension)
        {
            TypedInterpolationSlot? numeratorSlot = null;
            TypedInterpolationSlot? denominatorSlot = null;

            foreach (var slot in itc.Slots)
            {
                if (slot.SlotKind == InterpolationSlotKind.NumeratorUnit)
                    numeratorSlot = slot;
                else if (slot.SlotKind == InterpolationSlotKind.DenominatorUnit)
                    denominatorSlot = slot;
            }

            if (TryCreateCompoundQualifier(numeratorSlot, denominatorSlot, axis) is { } compoundQualifier)
                return compoundQualifier;

            if (denominatorSlot is not null)
                return CreateQualifierFromSlotExpression(denominatorSlot.Expression, axis);
        }

        return null;
    }

    private static DeclaredQualifierMeta? TryCreateCompoundQualifier(
        TypedInterpolationSlot? numeratorSlot,
        TypedInterpolationSlot? denominatorSlot,
        QualifierAxis axis)
    {
        if (numeratorSlot is null || denominatorSlot is null)
            return null;

        var numeratorQualifier = CreateQualifierFromSlotExpression(numeratorSlot.Expression, axis);
        var denominatorQualifier = CreateQualifierFromSlotExpression(denominatorSlot.Expression, axis);

        return (numeratorQualifier, denominatorQualifier) switch
        {
            (DeclaredQualifierMeta.Unit numerator, DeclaredQualifierMeta.Unit denominator) =>
                new DeclaredQualifierMeta.Unit(
                    $"{numerator.UnitCode}/{denominator.UnitCode}",
                    $"{numerator.DimensionName}/{denominator.DimensionName}"),
            (DeclaredQualifierMeta.Dimension numerator, DeclaredQualifierMeta.Dimension denominator) =>
                new DeclaredQualifierMeta.Dimension(
                    $"{numerator.DimensionName}/{denominator.DimensionName}"),
            _ => null,
        };
    }

    private static DeclaredQualifierMeta? CreateQualifierFromSlotExpression(
        TypedExpression expr, QualifierAxis axis)
    {
        var fieldName = expr switch
        {
            TypedFieldRef f => f.FieldName,
            TypedArgRef a => a.ArgName,
            _ => null,
        };

        if (fieldName is null)
            return null;

        return axis switch
        {
            QualifierAxis.Currency => new DeclaredQualifierMeta.Currency($"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.Unit => new DeclaredQualifierMeta.Unit($"{{{fieldName}}}", $"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension($"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.FromCurrency => new DeclaredQualifierMeta.FromCurrency($"{{{fieldName}}}", SourceFieldName: fieldName),
            QualifierAxis.ToCurrency => new DeclaredQualifierMeta.ToCurrency($"{{{fieldName}}}", SourceFieldName: fieldName),
            _ => null,
        };
    }

    private static DeclaredQualifierMeta? TryResolveCompoundCancellationUnit(
        TypedBinaryOp binOp, QualifierAxis axis, SemanticIndex semantics)
    {
        var leftQualifier = axis == QualifierAxis.Dimension
            ? ResolveQualifierFromExpression(binOp.Left, QualifierAxis.Unit, semantics)
                ?? ResolveQualifierFromExpression(binOp.Left, axis, semantics)
            : ResolveQualifierFromExpression(binOp.Left, axis, semantics);

        var rightQualifier = axis == QualifierAxis.Dimension
            ? ResolveQualifierFromExpression(binOp.Right, QualifierAxis.Unit, semantics)
                ?? ResolveQualifierFromExpression(binOp.Right, axis, semantics)
            : ResolveQualifierFromExpression(binOp.Right, axis, semantics);

        var compoundValue = ExtractCompoundValue(leftQualifier) ?? ExtractCompoundValue(rightQualifier);
        if (compoundValue is null)
            return null;

        var slashIndex = compoundValue.IndexOf('/');
        if (slashIndex < 0)
            return null;

        var numerator = compoundValue[..slashIndex].Trim();
        if (!TryDeriveCompoundNumeratorDimension(numerator, out var numeratorDimension))
            return null;

        return axis switch
        {
            QualifierAxis.Unit => new DeclaredQualifierMeta.Unit(numerator, numeratorDimension, QualifierOrigin.Derived),
            QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension(numeratorDimension, QualifierOrigin.Derived),
            _ => null,
        };
    }

    private static DeclaredQualifierMeta? TryResolveCompoundElevationDimension(
        TypedBinaryOp binOp, QualifierAxis axis, SemanticIndex semantics)
    {
        // Right operand is compound-quantity[Y/X]; elevation produces the numerator Y.
        var rightQualifier = axis == QualifierAxis.Dimension
            ? ResolveQualifierFromExpression(binOp.Right, QualifierAxis.Unit, semantics)
                ?? ResolveQualifierFromExpression(binOp.Right, axis, semantics)
            : ResolveQualifierFromExpression(binOp.Right, axis, semantics);

        var compoundValue = ExtractCompoundValue(rightQualifier);
        if (compoundValue is null)
            return null;

        var slashIndex = compoundValue.IndexOf('/');
        if (slashIndex < 0)
            return null;

        var numerator = compoundValue[..slashIndex].Trim();
        if (!TryDeriveCompoundNumeratorDimension(numerator, out var numeratorDimension))
            return null;

        return axis switch
        {
            QualifierAxis.Unit => new DeclaredQualifierMeta.Unit(numerator, numeratorDimension, QualifierOrigin.Derived),
            QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension(numeratorDimension, QualifierOrigin.Derived),
            _ => null,
        };
    }

    private static string? ExtractCompoundValue(DeclaredQualifierMeta? qualifier) => qualifier switch
    {
        DeclaredQualifierMeta.Unit { UnitCode: var code } when code.Contains('/') => code,
        DeclaredQualifierMeta.Dimension { DimensionName: var name } when name.Contains('/') => name,
        _ => null,
    };

    private static bool TryDeriveCompoundNumeratorDimension(string unitCode, out string dimensionName)
    {
        if (UnitDimensionHelper.CountQualifierUnitCodes.Contains(unitCode))
        {
            dimensionName = "count";
            return true;
        }

        if (unitCode.StartsWith("{", StringComparison.Ordinal)
            && unitCode.EndsWith("}", StringComparison.Ordinal)
            && unitCode.IndexOf('/') < 0)
        {
            dimensionName = $"{unitCode[..^1]}.dimension}}";
            return true;
        }

        var result = UcumParser.Parse(unitCode);
        if (result.IsValid)
        {
            dimensionName = UnitDimensionHelper.DeriveUnitDimensionName(result.Unit!);
            return !string.IsNullOrWhiteSpace(dimensionName);
        }

        dimensionName = string.Empty;
        return false;
    }

    /// <summary>Look up a field's qualifier on a specific axis (with standard fallbacks).</summary>
    private static DeclaredQualifierMeta? ResolveFieldQualifier(
        string fieldName, QualifierAxis axis, SemanticIndex semantics)
    {
        if (!semantics.FieldsByName.TryGetValue(fieldName, out var field))
            return null;

        foreach (var q in field.DeclaredQualifiers)
            if (q.Axis == axis) return q;

        if (axis == QualifierAxis.Unit)
            foreach (var q in field.DeclaredQualifiers)
                if (q.Axis == QualifierAxis.Dimension) return q;

        if (axis == QualifierAxis.Dimension)
            foreach (var q in field.DeclaredQualifiers)
                if (q.Axis == QualifierAxis.TemporalDimension) return q;

        // PriceIn fallback: CompoundPrice carries Currency, Unit, and Dimension components
        // (checked before implied qualifiers — explicit CompoundPrice is stronger than type-level implied)
        foreach (var q in field.DeclaredQualifiers)
        {
            var projected = TryProjectCompoundPrice(q, axis);
            if (projected is not null) return projected;
        }

        var typeMeta = Types.GetMeta(field.ResolvedType);
        foreach (var q in typeMeta.ImpliedQualifiers)
            if (q.Axis == axis) return q;

        return null;
    }

    /// <summary>
    /// Projects a <see cref="DeclaredQualifierMeta.CompoundPrice"/> onto a target axis.
    /// Returns the currency, unit, or dimension component as the appropriate subtype,
    /// or <c>null</c> if the qualifier is not a CompoundPrice or the axis is not applicable.
    /// </summary>
    private static DeclaredQualifierMeta? TryProjectCompoundPrice(DeclaredQualifierMeta qualifier, QualifierAxis axis)
    {
        if (qualifier is not DeclaredQualifierMeta.CompoundPrice compound)
            return null;

        return axis switch
        {
            QualifierAxis.Currency => new DeclaredQualifierMeta.Currency(
                compound.CurrencyCode, compound.Origin, compound.Preposition,
                compound.ProofSatisfactions, compound.SourceFieldName),
            QualifierAxis.Unit => new DeclaredQualifierMeta.Unit(
                compound.UnitCode, compound.DimensionName, compound.Origin, compound.Preposition,
                compound.ProofSatisfactions, compound.SourceFieldName),
            QualifierAxis.Dimension when !string.IsNullOrEmpty(compound.DimensionName) =>
                new DeclaredQualifierMeta.Dimension(
                    compound.DimensionName, compound.Origin, compound.Preposition,
                    compound.ProofSatisfactions, compound.SourceFieldName),
            _ => null,
        };
    }
}

