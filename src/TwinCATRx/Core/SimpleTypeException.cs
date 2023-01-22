﻿// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace CP.TwinCatRx.Core
{

    /// <summary>
    /// Exception thrown when a simple type is not supported.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class SimpleTypeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTypeException"/> class.
        /// </summary>
        public SimpleTypeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTypeException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SimpleTypeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTypeException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
        public SimpleTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTypeException"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected SimpleTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
