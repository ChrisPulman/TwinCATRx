// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.TwinCatRx.Core
{
    /// <summary>
    /// Invalid Variable Exception.
    /// </summary>
    [Serializable]
    public class InvalidVariableException : Exception
    {
        /// <summary>
        /// The variable.
        /// </summary>
        private readonly string? _variable;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVariableException"/> class.
        /// </summary>
        /// <param name="variable">The variable.</param>
        public InvalidVariableException(string? variable) => _variable = variable;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVariableException"/> class.
        /// </summary>
        public InvalidVariableException() => _variable = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVariableException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a null reference ( <see
        /// langword="Nothing"/> in Visual Basic) if no inner exception is specified.
        /// </param>
        public InvalidVariableException(string message, Exception innerException)
            : base(message, innerException) => _variable = message;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVariableException"/> class.
        /// </summary>
        /// <param name="info">
        /// The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the
        /// serialized object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains
        /// contextual information about the source or destination.
        /// </param>
        protected InvalidVariableException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) => _variable = string.Empty;

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message => $"The variable - {_variable} - does not exist in the PLC";
    }
}
