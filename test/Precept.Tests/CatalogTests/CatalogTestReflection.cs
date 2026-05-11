using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogTests;

internal static class CatalogTestReflection
{
    private const BindingFlags InstanceBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public static TheoryData<OperatorKind, OperatorMeta> AllOperators()
    {
        var data = new TheoryData<OperatorKind, OperatorMeta>();
        foreach (var meta in Operators.All)
        {
            data.Add(meta.Kind, meta);
        }

        return data;
    }

    public static TheoryData<OutcomeKind, OutcomeMeta> AllOutcomes()
    {
        var data = new TheoryData<OutcomeKind, OutcomeMeta>();
        foreach (var meta in Outcomes.All)
        {
            data.Add(meta.Kind, meta);
        }

        return data;
    }

    public static TheoryData<ModifierKind, ModifierMeta> AllModifiers()
    {
        var data = new TheoryData<ModifierKind, ModifierMeta>();
        foreach (var meta in Modifiers.All)
        {
            data.Add(meta.Kind, meta);
        }

        return data;
    }

    public static TheoryData<ModifierKind, ValueModifierMeta> AllValueModifiers()
    {
        var data = new TheoryData<ModifierKind, ValueModifierMeta>();
        foreach (var meta in Modifiers.All.OfType<ValueModifierMeta>())
        {
            data.Add(meta.Kind, meta);
        }

        return data;
    }

    public static TheoryData<TypeKind, TypeMeta> AllTypes()
    {
        var data = new TheoryData<TypeKind, TypeMeta>();
        foreach (var meta in Types.All)
        {
            data.Add(meta.Kind, meta);
        }

        return data;
    }

    public static TheoryData<DiagnosticCode, DiagnosticMeta> AllDiagnostics()
    {
        var data = new TheoryData<DiagnosticCode, DiagnosticMeta>();
        foreach (var code in Enum.GetValues<DiagnosticCode>())
        {
            data.Add(code, Diagnostics.GetMeta(code));
        }

        return data;
    }

    public static string ReadOperatorSymbol(OperatorMeta meta)
    {
        if (TryGetPropertyValue(meta, "Symbol", out var symbolValue))
        {
            return symbolValue as string ?? string.Empty;
        }

        if (TryGetPropertyValue(meta, "Token", out var tokenValue) && tokenValue is TokenMeta token)
        {
            return token.Text ?? string.Empty;
        }

        if (TryGetPropertyValue(meta, "Tokens", out var tokensValue) && tokensValue is IEnumerable tokens)
        {
            var parts = tokens.Cast<object>()
                .Select(tokenObject => tokenObject is TokenMeta tokenMeta
                    ? tokenMeta.Text
                    : ReadStringProperty(tokenObject, "Text"))
                .Where(text => !string.IsNullOrWhiteSpace(text));

            return string.Join(" ", parts!);
        }

        throw new InvalidOperationException($"{meta.GetType().Name} does not expose Symbol, Token, or Tokens.");
    }

    public static string ReadModifierKeyword(ModifierMeta meta)
    {
        if (TryGetPropertyValue(meta, "Keyword", out var keywordValue))
        {
            return keywordValue as string ?? string.Empty;
        }

        return ReadTokenText(meta, "Token");
    }

    public static IReadOnlyList<TypeTarget> ReadApplicableTypes(ValueModifierMeta meta)
    {
        foreach (var propertyName in new[] { "ApplicableTypes", "ApplicableTo" })
        {
            if (!TryGetPropertyValue(meta, propertyName, out var value))
            {
                continue;
            }

            if (value is IEnumerable<TypeTarget> typedTargets)
            {
                return typedTargets.ToArray();
            }

            if (value is IEnumerable untypedTargets)
            {
                return untypedTargets.Cast<TypeTarget>().ToArray();
            }

            throw new InvalidOperationException($"{meta.GetType().Name}.{propertyName} is not an IEnumerable<TypeTarget>.");
        }

        throw new InvalidOperationException($"{meta.GetType().Name} does not expose ApplicableTypes or ApplicableTo.");
    }

    public static string ReadTypeSurfaceName(TypeMeta meta)
        => ReadFirstExistingStringProperty(meta, "SerializedName", "DisplayName");

    public static IReadOnlyList<string> ReadDiagnosticRecoveryGuidance(DiagnosticMeta meta)
    {
        if (TryGetPropertyValue(meta, "RecoveryHint", out var recoveryHintValue))
        {
            return [recoveryHintValue as string ?? string.Empty];
        }

        if (TryGetPropertyValue(meta, "RecoverySteps", out var recoveryStepsValue) && recoveryStepsValue is IEnumerable recoverySteps)
        {
            return recoverySteps.Cast<object?>()
                .Select(step => step as string ?? string.Empty)
                .ToArray();
        }

        if (TryGetPropertyValue(meta, "FixHint", out var fixHintValue))
        {
            return [fixHintValue as string ?? string.Empty];
        }

        throw new InvalidOperationException($"{meta.GetType().Name} does not expose RecoveryHint, RecoverySteps, or FixHint.");
    }

    public static bool TryGetPropertyValue(object instance, string propertyName, out object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, InstanceBindings);
        if (property is null)
        {
            value = null;
            return false;
        }

        value = property.GetValue(instance);
        return true;
    }

    public static string ReadTokenText(object instance, string propertyName)
    {
        if (TryGetPropertyValue(instance, propertyName, out var value) && value is TokenMeta token)
        {
            return token.Text ?? string.Empty;
        }

        throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} is missing or is not TokenMeta.");
    }

    private static string ReadFirstExistingStringProperty(object instance, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetPropertyValue(instance, propertyName, out var value))
            {
                return value as string ?? string.Empty;
            }
        }

        throw new InvalidOperationException($"{instance.GetType().Name} does not expose any of: {string.Join(", ", propertyNames)}.");
    }

    private static string ReadStringProperty(object instance, string propertyName)
        => ReadFirstExistingStringProperty(instance, propertyName);
}
