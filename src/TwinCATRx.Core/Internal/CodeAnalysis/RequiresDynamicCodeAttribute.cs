// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Stub for RequiresDynamicCodeAttribute on older TFMs.
/// </summary>
[AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Constructor | System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = false)]
public sealed class RequiresDynamicCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresDynamicCodeAttribute"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public RequiresDynamicCodeAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    public string? Url { get; set; }
}
#endif
