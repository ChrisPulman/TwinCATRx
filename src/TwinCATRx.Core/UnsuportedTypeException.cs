﻿// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.TwinCatRx.Core;

/// <summary>
/// Exception thrown when a simple type is not supported.
/// </summary>
/// <seealso cref="System.Exception" />
[Serializable]
public class UnsuportedTypeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsuportedTypeException"/> class.
    /// </summary>
    public UnsuportedTypeException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsuportedTypeException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnsuportedTypeException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsuportedTypeException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
    public UnsuportedTypeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
