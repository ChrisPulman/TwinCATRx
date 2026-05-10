// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TwinCATRx.Tests;

internal static class TestAssert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "Expected condition to be true.");
        }
    }

    public static void False(bool condition, string? message = null) => True(!condition, message ?? "Expected condition to be false.");

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(message ?? $"Expected '{expected}', but found '{actual}'.");
        }
    }

    public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException("Expected sequences to be equal.");
        }
    }

    public static void NotNull(object? value, string? message = null) => True(value != null, message ?? "Expected value to be non-null.");

    public static void Null(object? value, string? message = null) => True(value == null, message ?? "Expected value to be null.");

    public static void Empty<T>(IEnumerable<T> values) => True(!values.Any(), "Expected sequence to be empty.");

    public static void NotEmpty<T>(IEnumerable<T> values) => True(values.Any(), "Expected sequence to be non-empty.");

    public static void Count<T>(int expected, ICollection<T> values) => Equal(expected, values.Count);

    public static void Length<T>(int expected, T[] values) => Equal(expected, values.Length);

    public static void Contains<T>(T expected, IEnumerable<T> values) => True(values.Contains(expected), $"Expected sequence to contain '{expected}'.");

    public static void DoesNotContain<T>(T expected, IEnumerable<T> values) => True(!values.Contains(expected), $"Expected sequence not to contain '{expected}'.");

    public static void StartsWith(string expected, string actual) => True(actual.StartsWith(expected, StringComparison.Ordinal), $"Expected '{actual}' to start with '{expected}'.");

    public static void NotSame(object expected, object actual) => True(!ReferenceEquals(expected, actual), "Expected references to be different.");

    public static TException Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex)
        {
            return ex;
        }

        throw new InvalidOperationException($"Expected exception of type {typeof(TException).FullName}.");
    }
}
