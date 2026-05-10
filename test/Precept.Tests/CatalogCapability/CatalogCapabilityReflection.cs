using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;

namespace Precept.Tests.CatalogCapability;

internal static class CatalogCapabilityReflection
{
    private const BindingFlags AllBindings =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public static object? GetInstanceValue(object instance, string propertyName)
        => RequireProperty(instance.GetType(), propertyName).GetValue(instance);

    public static object? GetStaticValue(Type type, string propertyName)
        => RequireProperty(type, propertyName).GetValue(null);

    public static IEnumerable<T> GetStaticSequence<T>(Type type, string propertyName)
    {
        var value = GetStaticValue(type, propertyName);
        value.Should().BeAssignableTo<IEnumerable<T>>(
            $"{type.Name}.{propertyName} must expose a {typeof(T).Name} sequence");
        return (IEnumerable<T>)value!;
    }

    private static PropertyInfo RequireProperty(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, AllBindings);
        property.Should().NotBeNull(
            $"{type.FullName} must expose '{propertyName}' for Track 2 Phase A catalog coverage");
        return property!;
    }
}
