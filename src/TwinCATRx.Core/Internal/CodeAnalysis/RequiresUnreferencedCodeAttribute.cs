// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Stub for RequiresUnreferencedCodeAttribute on older TFMs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RequiresUnreferencedCodeAttribute"/> class.
/// </remarks>
/// <param name="message">The message.</param>
[AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Constructor | System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = false)]
public sealed class RequiresUnreferencedCodeAttribute(string message) : Attribute
{
    /// <summary>
    /// Gets the message.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    public string? Url { get; set; }
}
#endif
