using System.Collections.Immutable;
using System.Linq;
using Precept.Language;

namespace Precept.Pipeline;

internal enum QualifierResolutionKind
{
    Resolved = 1,
    Unknown = 2,
    Absent = 3,
}

internal sealed record ResolvedQualifierAxis(
    QualifierAxis Axis,
    QualifierResolutionKind Kind,
    DeclaredQualifierMeta? Qualifier);

internal static partial class TypeChecker
{
    private static void ValidateAssignmentQualifiers(
        TypedExpression value,
        string fieldName,
        ImmutableArray<DeclaredQualifierMeta> targetQualifiers,
        SourceSpan valueSpan,
        CheckContext ctx)
    {
        if (targetQualifiers.IsDefaultOrEmpty || value is TypedErrorExpression)
            return;

        if (value is TypedTypedConstant { ResultType: TypeKind.Quantity })
            return;

        if (value is TypedConditional conditional)
        {
            ValidateAssignmentQualifiers(conditional.ThenBranch, fieldName, targetQualifiers, valueSpan, ctx);
            ValidateAssignmentQualifiers(conditional.ElseBranch, fieldName, targetQualifiers, valueSpan, ctx);
            return;
        }

        var requiredAxes = ExpandAssignmentTargetQualifiers(targetQualifiers);
        var sourceAxes = requiredAxes
            .Select(static target => target.Axis)
            .Distinct()
            .Select(axis => ResolveAssignmentQualifierAxis(value, axis))
            .ToImmutableArray();

        ValidateResolvedQualifierAxes(sourceAxes, fieldName, requiredAxes, valueSpan, ctx);
    }

    private static ImmutableArray<DeclaredQualifierMeta> ExpandAssignmentTargetQualifiers(
        ImmutableArray<DeclaredQualifierMeta> targetQualifiers)
    {
        if (targetQualifiers.IsDefaultOrEmpty)
            return [];

        var builder = ImmutableArray.CreateBuilder<DeclaredQualifierMeta>(targetQualifiers.Length + 1);

        foreach (var qualifier in targetQualifiers)
        {
            switch (qualifier)
            {
                case DeclaredQualifierMeta.CompoundPrice compound:
                    builder.Add(new DeclaredQualifierMeta.Currency(
                        compound.CurrencyCode,
                        compound.Origin,
                        TokenKind.In,
                        compound.ProofSatisfactions,
                        compound.SourceFieldName));
                    builder.Add(new DeclaredQualifierMeta.Unit(
                        compound.UnitCode,
                        compound.DimensionName,
                        compound.Origin,
                        TokenKind.In,
                        compound.ProofSatisfactions,
                        compound.SourceFieldName));
                    break;

                default:
                    builder.Add(qualifier);
                    break;
            }
        }

        return builder.ToImmutable();
    }

    private static void ValidateResolvedQualifierAxes(
        ImmutableArray<ResolvedQualifierAxis> sourceAxes,
        string fieldName,
        ImmutableArray<DeclaredQualifierMeta> targetQualifiers,
        SourceSpan valueSpan,
        CheckContext ctx)
    {
        foreach (var targetQualifier in targetQualifiers)
        {
            var resolution = sourceAxes.First(axis => axis.Axis == targetQualifier.Axis);
            switch (resolution.Kind)
            {
                case QualifierResolutionKind.Resolved when resolution.Qualifier is not null:
                    if (!ResolvedQualifierSatisfiesTarget(resolution.Qualifier, targetQualifier))
                    {
                        EmitAssignmentQualifierMismatch(targetQualifier, resolution.Qualifier, fieldName, valueSpan, ctx);
                    }

                    break;

                case QualifierResolutionKind.Unknown:
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(
                            DiagnosticCode.UnprovedAssignmentQualifierCompatibility,
                            valueSpan,
                            FormatQualifierAxisName(targetQualifier.Axis),
                            fieldName));
                    break;
            }
        }
    }

    private static ResolvedQualifierAxis ResolveAssignmentQualifierAxis(TypedExpression value, QualifierAxis axis)
    {
        switch (value)
        {
            case TypedFieldRef fieldRef:
                return ResolveDirectQualifierAxis(fieldRef.ResultType, fieldRef.DeclaredQualifiers, axis);

            case TypedArgRef argRef:
                return ResolveDirectQualifierAxis(argRef.ResultType, argRef.DeclaredQualifiers, axis);

            case TypedTypedConstant typedConstant:
                return ResolveTypedConstantQualifierAxis(typedConstant, axis);

            case InterpolatedTypedConstant interpolated:
                return ResolveInterpolatedQualifierAxis(interpolated, axis);

            case TypedFunctionCall functionCall:
                return ResolveFunctionCallQualifierAxis(functionCall, axis);

            case TypedUnaryOp unary:
                return ResolveAssignmentQualifierAxis(unary.Operand, axis);

            case TypedBinaryOp { ResultQualifier: { } } binary:
                return ResolveBinaryQualifierAxis(binary, axis);

            default:
                return new(axis, QualifierResolutionKind.Absent, null);
        }
    }

    private static ResolvedQualifierAxis ResolveDirectQualifierAxis(
        TypeKind resultType,
        ImmutableArray<DeclaredQualifierMeta>? declaredQualifiers,
        QualifierAxis axis)
    {
        var qualifiers = declaredQualifiers ?? default;
        if (!qualifiers.IsDefaultOrEmpty)
        {
            foreach (var qualifier in qualifiers)
            {
                if (ProjectQualifierForAxis(qualifier, axis) is { } projected)
                    return new(axis, QualifierResolutionKind.Resolved, projected);
            }
        }

        foreach (var impliedQualifier in Types.GetMeta(resultType).ImpliedQualifiers)
        {
            if (ProjectQualifierForAxis(impliedQualifier, axis) is { } projected)
                return new(axis, QualifierResolutionKind.Resolved, projected);
        }

        return IsAssignmentQualifierAxisApplicable(resultType, axis)
            ? new(axis, QualifierResolutionKind.Unknown, null)
            : new(axis, QualifierResolutionKind.Absent, null);
    }

    private static ResolvedQualifierAxis ResolveFunctionCallQualifierAxis(TypedFunctionCall functionCall, QualifierAxis axis)
    {
        if (functionCall.ResultQualifiers is { } resultQualifiers && !resultQualifiers.IsDefaultOrEmpty)
        {
            foreach (var qualifier in resultQualifiers)
            {
                if (ProjectQualifierForAxis(qualifier, axis) is { } projected)
                    return new(axis, QualifierResolutionKind.Resolved, projected);
            }
        }

        return IsAssignmentQualifierAxisApplicable(functionCall.ResultType, axis)
            ? new(axis, QualifierResolutionKind.Unknown, null)
            : new(axis, QualifierResolutionKind.Absent, null);
    }

    private static ResolvedQualifierAxis ResolveTypedConstantQualifierAxis(
        TypedTypedConstant typedConstant,
        QualifierAxis axis)
    {
        var direct = ResolveDirectQualifierAxis(typedConstant.ResultType, typedConstant.DeclaredQualifiers, axis);
        var declaredQualifiers = typedConstant.DeclaredQualifiers ?? default;
        if (direct.Kind == QualifierResolutionKind.Resolved
            || !declaredQualifiers.IsDefaultOrEmpty
            || !IsAssignmentQualifierAxisApplicable(typedConstant.ResultType, axis))
        {
            return direct;
        }

        if (TryResolveTypedConstantTextAxis(typedConstant, axis, out var parsedQualifier))
            return new(axis, QualifierResolutionKind.Resolved, parsedQualifier);

        return direct;
    }

    private static bool TryResolveTypedConstantTextAxis(
        TypedTypedConstant typedConstant,
        QualifierAxis axis,
        out DeclaredQualifierMeta qualifier)
    {
        qualifier = null!;

        if (TryExtractQualifiedPayload(typedConstant.RawText, out var payload))
        {
            switch (typedConstant.ResultType)
            {
                case TypeKind.Money when axis == QualifierAxis.Currency && TryExtractCurrency(payload, out var currencyCode):
                    qualifier = new DeclaredQualifierMeta.Currency(currencyCode);
                    return true;

                case TypeKind.Price when TryExtractCurrencyAndUnit(payload, out var priceCurrency, out var priceUnit):
                    qualifier = axis switch
                    {
                        QualifierAxis.Currency => new DeclaredQualifierMeta.Currency(priceCurrency),
                        QualifierAxis.Unit => new DeclaredQualifierMeta.Unit(priceUnit.CanonicalCode, UnitDimensionHelper.DeriveUnitDimensionName(priceUnit)),
                        QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension(UnitDimensionHelper.DeriveUnitDimensionName(priceUnit)),
                        _ => null!,
                    };
                    return qualifier is not null;

                case TypeKind.ExchangeRate when TryExtractFromToCurrencies(payload, out var fromCode, out var toCode):
                    qualifier = axis switch
                    {
                        QualifierAxis.FromCurrency => new DeclaredQualifierMeta.FromCurrency(fromCode),
                        QualifierAxis.ToCurrency => new DeclaredQualifierMeta.ToCurrency(toCode),
                        _ => null!,
                    };
                    return qualifier is not null;
            }
        }

        return false;
    }

    private static bool TryExtractQualifiedPayload(string rawText, out string payload)
    {
        payload = string.Empty;

        var trimmed = rawText.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex <= 0 || spaceIndex == trimmed.Length - 1)
            return false;

        payload = trimmed[(spaceIndex + 1)..].Trim();
        return payload.Length > 0;
    }

    private static ResolvedQualifierAxis ResolveInterpolatedQualifierAxis(
        InterpolatedTypedConstant interpolated,
        QualifierAxis axis)
    {
        var wholeValueSlot = interpolated.Slots.FirstOrDefault(slot => slot.SlotKind == InterpolationSlotKind.WholeValue);
        if (wholeValueSlot is not null)
            return ResolveAssignmentQualifierAxis(wholeValueSlot.Expression, axis);

        if (TryResolveStaticInterpolatedAxis(interpolated.StaticQualifier, axis, out var staticQualifier))
            return new(axis, QualifierResolutionKind.Resolved, staticQualifier);

        if (axis == QualifierAxis.Currency
            && interpolated.ResultType == TypeKind.Price
            && TryExtractInterpolatedPriceCurrency(interpolated.StaticText, out var currencyCode))
        {
            return new(axis, QualifierResolutionKind.Resolved, new DeclaredQualifierMeta.Currency(currencyCode));
        }

        return axis switch
        {
            QualifierAxis.Currency => ResolveInterpolatedSlotAxis(interpolated, InterpolationSlotKind.Currency, axis),
            QualifierAxis.FromCurrency => ResolveInterpolatedSlotAxis(interpolated, InterpolationSlotKind.FromCurrency, axis),
            QualifierAxis.ToCurrency => ResolveInterpolatedSlotAxis(interpolated, InterpolationSlotKind.ToCurrency, axis),
            QualifierAxis.Unit or QualifierAxis.Dimension => ResolveInterpolatedSlotAxis(interpolated, InterpolationSlotKind.Unit, axis),
            _ => new(axis, QualifierResolutionKind.Absent, null),
        };
    }

    private static ResolvedQualifierAxis ResolveInterpolatedSlotAxis(
        InterpolatedTypedConstant interpolated,
        InterpolationSlotKind slotKind,
        QualifierAxis axis)
    {
        var slot = interpolated.Slots.FirstOrDefault(candidate => candidate.SlotKind == slotKind);
        if (slot is null)
            return new(axis, QualifierResolutionKind.Absent, null);

        return ResolveSlotSourceQualifierAxis(slot.Expression, axis, out _);
    }

    private static ResolvedQualifierAxis ResolveSlotSourceQualifierAxis(
        TypedExpression holeExpression,
        QualifierAxis axis,
        out string sourceName)
    {
        sourceName = GetQualifierSourceName(holeExpression);

        if (holeExpression is not TypedMemberAccess
            {
                Object: var source,
                ResolvedAccessor: FixedReturnAccessor { ReturnsQualifier: var returnsQualifier }
            }
            || !SlotAccessorCanResolveAxis(returnsQualifier, axis))
        {
            return new(axis, QualifierResolutionKind.Absent, null);
        }

        return source switch
        {
            TypedFieldRef fieldRef => ResolveDirectQualifierAxis(fieldRef.ResultType, fieldRef.DeclaredQualifiers, axis),
            TypedArgRef argRef => ResolveDirectQualifierAxis(argRef.ResultType, argRef.DeclaredQualifiers, axis),
            _ => IsAssignmentQualifierAxisApplicable(source.ResultType, axis)
                ? new(axis, QualifierResolutionKind.Unknown, null)
                : new(axis, QualifierResolutionKind.Absent, null),
        };
    }

    private static ResolvedQualifierAxis ResolveBinaryQualifierAxis(TypedBinaryOp binary, QualifierAxis axis)
    {
        return binary.ResultQualifier switch
        {
            SameQualifierRequired => ResolveSameQualifierBinaryAxis(binary, axis),
            QualifiedOperandInherited => ResolveAssignmentQualifierAxis(
                binary.Left.ResultType == binary.ResultType ? binary.Left : binary.Right,
                axis),
            CurrencyConversionRequired => ResolveCurrencyConversionAxis(binary, axis),
            CompoundDimensionElevationRequired => ResolveCompoundElevationAxis(binary, axis),
            CompoundUnitCancellationRequired => ResolveCompoundCancellationAxis(binary, axis),
            _ => new(axis, QualifierResolutionKind.Absent, null),
        };
    }

    private static ResolvedQualifierAxis ResolveSameQualifierBinaryAxis(TypedBinaryOp binary, QualifierAxis axis)
    {
        var left = ResolveAssignmentQualifierAxis(binary.Left, axis);
        var right = ResolveAssignmentQualifierAxis(binary.Right, axis);

        if (left.Kind == QualifierResolutionKind.Unknown || right.Kind == QualifierResolutionKind.Unknown)
            return new(axis, QualifierResolutionKind.Unknown, null);

        if (left.Kind == QualifierResolutionKind.Resolved && right.Kind == QualifierResolutionKind.Resolved)
        {
            return left.Qualifier is not null && right.Qualifier is not null && ResolvedQualifiersCompatible(left.Qualifier, right.Qualifier, axis)
                ? left
                : new(axis, QualifierResolutionKind.Absent, null);
        }

        return new(axis, QualifierResolutionKind.Absent, null);
    }

    private static ResolvedQualifierAxis ResolveCurrencyConversionAxis(TypedBinaryOp binary, QualifierAxis axis)
    {
        if (axis != QualifierAxis.Currency)
            return new(axis, QualifierResolutionKind.Absent, null);

        var rateOperand = binary.Left.ResultType == TypeKind.ExchangeRate ? binary.Left : binary.Right;
        var toCurrency = ResolveAssignmentQualifierAxis(rateOperand, QualifierAxis.ToCurrency);
        return toCurrency.Kind switch
        {
            QualifierResolutionKind.Resolved when toCurrency.Qualifier is DeclaredQualifierMeta.ToCurrency qualifier => new(
                axis,
                QualifierResolutionKind.Resolved,
                new DeclaredQualifierMeta.Currency(
                    qualifier.CurrencyCode,
                    qualifier.Origin,
                    TokenKind.In,
                    qualifier.ProofSatisfactions,
                    qualifier.SourceFieldName)),
            QualifierResolutionKind.Unknown => new(axis, QualifierResolutionKind.Unknown, null),
            _ => new(axis, QualifierResolutionKind.Absent, null),
        };
    }

    private static ResolvedQualifierAxis ResolveCompoundElevationAxis(TypedBinaryOp binary, QualifierAxis axis)
    {
        var priceOperand = binary.Left.ResultType == TypeKind.Price ? binary.Left : binary.Right;
        var quantityOperand = ReferenceEquals(priceOperand, binary.Left) ? binary.Right : binary.Left;

        return axis switch
        {
            QualifierAxis.Currency => ResolveAssignmentQualifierAxis(priceOperand, axis),
            QualifierAxis.Unit or QualifierAxis.Dimension => TryResolveCompoundNumeratorAxis(quantityOperand, axis, out var resolved)
                ? resolved
                : new(axis, QualifierResolutionKind.Unknown, null),
            _ => new(axis, QualifierResolutionKind.Absent, null),
        };
    }

    private static ResolvedQualifierAxis ResolveCompoundCancellationAxis(TypedBinaryOp binary, QualifierAxis axis)
    {
        if (axis is not (QualifierAxis.Unit or QualifierAxis.Dimension))
            return new(axis, QualifierResolutionKind.Absent, null);

        if (TryMatchCompoundUnitCancellationAxis(binary.Left, binary.Right, axis, out var resolved)
            || TryMatchCompoundUnitCancellationAxis(binary.Right, binary.Left, axis, out resolved))
        {
            return resolved;
        }

        if (HasDefiniteCompoundCancellationMismatch(binary.Left, binary.Right)
            || HasDefiniteCompoundCancellationMismatch(binary.Right, binary.Left))
        {
            return new(axis, QualifierResolutionKind.Absent, null);
        }

        return new(axis, QualifierResolutionKind.Unknown, null);
    }

    private static bool TryMatchCompoundUnitCancellationAxis(
        TypedExpression standaloneQuantity,
        TypedExpression compoundQuantity,
        QualifierAxis axis,
        out ResolvedQualifierAxis resolved)
    {
        if (!TryMatchCompoundUnitCancellation(standaloneQuantity, compoundQuantity, out var resultUnit))
        {
            resolved = new(axis, QualifierResolutionKind.Unknown, null);
            return false;
        }

        resolved = axis == QualifierAxis.Unit
            ? new(axis, QualifierResolutionKind.Resolved, resultUnit)
            : new(axis, QualifierResolutionKind.Resolved, new DeclaredQualifierMeta.Dimension(
                resultUnit.DimensionName,
                resultUnit.Origin,
                TokenKind.Of,
                resultUnit.ProofSatisfactions,
                resultUnit.SourceFieldName));
        return true;
    }

    private static bool HasDefiniteCompoundCancellationMismatch(
        TypedExpression standaloneQuantity,
        TypedExpression compoundQuantity)
    {
        return TryGetQuantityDimensionName(standaloneQuantity, out var standaloneDimension)
            && TryGetCompoundUnit(compoundQuantity, out var compoundUnit)
            && TrySplitCompoundUnit(compoundUnit.UnitCode, out _, out var denominatorUnit)
            && TryDeriveUnitDimensionName(denominatorUnit, out var denominatorDimension)
            && !StringComparer.OrdinalIgnoreCase.Equals(standaloneDimension, denominatorDimension);
    }

    private static bool TryResolveCompoundNumeratorAxis(
        TypedExpression quantityOperand,
        QualifierAxis axis,
        out ResolvedQualifierAxis resolved)
    {
        if (!TryGetCompoundUnit(quantityOperand, out var compoundUnit)
            || !TrySplitCompoundUnit(compoundUnit.UnitCode, out var numeratorUnit, out _)
            || !TryDeriveUnitDimensionName(numeratorUnit, out var numeratorDimension))
        {
            resolved = new(axis, QualifierResolutionKind.Unknown, null);
            return false;
        }

        resolved = axis == QualifierAxis.Unit
            ? new(axis, QualifierResolutionKind.Resolved, new DeclaredQualifierMeta.Unit(
                numeratorUnit,
                numeratorDimension,
                QualifierOrigin.Derived,
                TokenKind.In,
                compoundUnit.ProofSatisfactions,
                compoundUnit.SourceFieldName))
            : new(axis, QualifierResolutionKind.Resolved, new DeclaredQualifierMeta.Dimension(
                numeratorDimension,
                QualifierOrigin.Derived,
                TokenKind.Of,
                compoundUnit.ProofSatisfactions,
                compoundUnit.SourceFieldName));
        return true;
    }

    private static bool TryResolveStaticInterpolatedAxis(
        StaticInterpolatedQualifier? staticQualifier,
        QualifierAxis axis,
        out DeclaredQualifierMeta qualifier)
    {
        qualifier = null!;
        if (staticQualifier is null)
            return false;

        DeclaredQualifierMeta? projected = staticQualifier switch
        {
            StaticCurrencyQualifier { CurrencyCode: var code } when axis == QualifierAxis.Currency =>
                new DeclaredQualifierMeta.Currency(code),
            StaticUnitQualifier { Unit: var unit } when axis == QualifierAxis.Unit =>
                new DeclaredQualifierMeta.Unit(unit.CanonicalCode, UnitDimensionHelper.DeriveUnitDimensionName(unit)),
            StaticUnitQualifier { Unit: var unit } when axis == QualifierAxis.Dimension =>
                new DeclaredQualifierMeta.Dimension(UnitDimensionHelper.DeriveUnitDimensionName(unit)),
            StaticCurrencyAndUnitQualifier { CurrencyCode: var code } when axis == QualifierAxis.Currency =>
                new DeclaredQualifierMeta.Currency(code),
            StaticCurrencyAndUnitQualifier { Unit: var unit } when axis == QualifierAxis.Unit =>
                new DeclaredQualifierMeta.Unit(unit.CanonicalCode, UnitDimensionHelper.DeriveUnitDimensionName(unit)),
            StaticCurrencyAndUnitQualifier { Unit: var unit } when axis == QualifierAxis.Dimension =>
                new DeclaredQualifierMeta.Dimension(UnitDimensionHelper.DeriveUnitDimensionName(unit)),
            StaticFromToCurrenciesQualifier { FromCode: var fromCode } when axis == QualifierAxis.FromCurrency =>
                new DeclaredQualifierMeta.FromCurrency(fromCode),
            StaticFromToCurrenciesQualifier { ToCode: var toCode } when axis == QualifierAxis.ToCurrency =>
                new DeclaredQualifierMeta.ToCurrency(toCode),
            _ => null,
        };

        if (projected is null)
            return false;

        qualifier = projected;
        return true;
    }

    private static DeclaredQualifierMeta? ProjectQualifierForAxis(DeclaredQualifierMeta qualifier, QualifierAxis axis)
    {
        if (qualifier.Axis == axis)
            return qualifier;

        if (qualifier is DeclaredQualifierMeta.Unit unit && axis == QualifierAxis.Dimension)
        {
            return new DeclaredQualifierMeta.Dimension(
                unit.DimensionName,
                unit.Origin,
                TokenKind.Of,
                unit.ProofSatisfactions,
                unit.SourceFieldName);
        }

        if (qualifier is not DeclaredQualifierMeta.CompoundPrice compound)
            return null;

        return axis switch
        {
            QualifierAxis.Currency => new DeclaredQualifierMeta.Currency(
                compound.CurrencyCode,
                compound.Origin,
                TokenKind.In,
                compound.ProofSatisfactions,
                compound.SourceFieldName),
            QualifierAxis.Unit => new DeclaredQualifierMeta.Unit(
                compound.UnitCode,
                compound.DimensionName,
                compound.Origin,
                TokenKind.In,
                compound.ProofSatisfactions,
                compound.SourceFieldName),
            QualifierAxis.Dimension => new DeclaredQualifierMeta.Dimension(
                compound.DimensionName,
                compound.Origin,
                TokenKind.Of,
                compound.ProofSatisfactions,
                compound.SourceFieldName),
            _ => null,
        };
    }

    private static bool ResolvedQualifierSatisfiesTarget(DeclaredQualifierMeta sourceQualifier, DeclaredQualifierMeta targetQualifier) =>
        targetQualifier.Axis switch
        {
            QualifierAxis.Dimension => TryGetQualifierText(sourceQualifier, QualifierAxis.Dimension, out var sourceDimension)
                && TryGetQualifierText(targetQualifier, QualifierAxis.Dimension, out var targetDimension)
                && StringComparer.OrdinalIgnoreCase.Equals(sourceDimension, targetDimension),
            _ => TryGetQualifierText(sourceQualifier, targetQualifier.Axis, out var sourceValue)
                && TryGetQualifierText(targetQualifier, targetQualifier.Axis, out var targetValue)
                && StringComparer.OrdinalIgnoreCase.Equals(sourceValue, targetValue),
        };

    private static bool ResolvedQualifiersCompatible(DeclaredQualifierMeta leftQualifier, DeclaredQualifierMeta rightQualifier, QualifierAxis axis) =>
        TryGetQualifierText(leftQualifier, axis, out var leftValue)
        && TryGetQualifierText(rightQualifier, axis, out var rightValue)
        && StringComparer.OrdinalIgnoreCase.Equals(leftValue, rightValue);

    private static void EmitAssignmentQualifierMismatch(
        DeclaredQualifierMeta targetQualifier,
        DeclaredQualifierMeta sourceQualifier,
        string fieldName,
        SourceSpan valueSpan,
        CheckContext ctx)
    {
        if (targetQualifier.Axis == QualifierAxis.Dimension)
        {
            if (TryGetQualifierText(sourceQualifier, QualifierAxis.Dimension, out var sourceDimension)
                && TryGetQualifierText(targetQualifier, QualifierAxis.Dimension, out var targetDimension))
            {
                ctx.Diagnostics.Add(
                    Diagnostics.Create(
                        DiagnosticCode.DimensionCategoryMismatch,
                        valueSpan,
                        sourceDimension,
                        targetDimension,
                        fieldName));
            }

            return;
        }

        if (TryGetQualifierText(targetQualifier, targetQualifier.Axis, out var targetValue))
        {
            ctx.Diagnostics.Add(
                Diagnostics.Create(
                    DiagnosticCode.QualifierMismatch,
                    valueSpan,
                    targetValue,
                    fieldName));
        }
    }

    private static bool TryGetQualifierText(DeclaredQualifierMeta qualifier, QualifierAxis axis, out string value)
    {
        value = string.Empty;
        if (ProjectQualifierForAxis(qualifier, axis) is not { } projected)
            return false;

        value = projected switch
        {
            DeclaredQualifierMeta.Currency currency => currency.CurrencyCode,
            DeclaredQualifierMeta.Unit unit => unit.UnitCode,
            DeclaredQualifierMeta.Dimension dimension => dimension.DimensionName,
            DeclaredQualifierMeta.FromCurrency fromCurrency => fromCurrency.CurrencyCode,
            DeclaredQualifierMeta.ToCurrency toCurrency => toCurrency.CurrencyCode,
            _ => string.Empty,
        };

        return !string.IsNullOrWhiteSpace(value);
    }

    private static ImmutableArray<QualifierAxis> GetApplicableAssignmentQualifierAxes(TypeKind resultType) => resultType switch
    {
        TypeKind.Money => [QualifierAxis.Currency],
        TypeKind.Quantity => [QualifierAxis.Unit, QualifierAxis.Dimension],
        TypeKind.Price => [QualifierAxis.Currency, QualifierAxis.Unit, QualifierAxis.Dimension],
        TypeKind.ExchangeRate => [QualifierAxis.FromCurrency, QualifierAxis.ToCurrency],
        _ => [],
    };

    private static bool IsAssignmentQualifierAxisApplicable(TypeKind resultType, QualifierAxis axis) =>
        GetApplicableAssignmentQualifierAxes(resultType).Contains(axis);

    private static bool SlotAccessorCanResolveAxis(QualifierAxis returnsQualifier, QualifierAxis targetAxis) =>
        returnsQualifier == targetAxis
        || (returnsQualifier == QualifierAxis.Unit && targetAxis == QualifierAxis.Dimension);

    private static string GetQualifierSourceName(TypedExpression expression) => expression switch
    {
        TypedMemberAccess { Object: TypedFieldRef { FieldName: var fieldName } } => fieldName,
        TypedMemberAccess { Object: TypedArgRef { ArgName: var argName } } => argName,
        TypedFieldRef { FieldName: var fieldName } => fieldName,
        TypedArgRef { ArgName: var argName } => argName,
        _ => string.Empty,
    };

    private static string FormatQualifierAxisName(QualifierAxis axis) => axis switch
    {
        QualifierAxis.Currency => "currency",
        QualifierAxis.Unit => "unit",
        QualifierAxis.Dimension => "dimension",
        QualifierAxis.FromCurrency => "from currency",
        QualifierAxis.ToCurrency => "to currency",
        _ => axis.ToString().ToLowerInvariant(),
    };
}
