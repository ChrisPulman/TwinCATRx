// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Stub for UnconditionalSuppressMessageAttribute on older TFMs.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class UnconditionalSuppressMessageAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnconditionalSuppressMessageAttribute"/> class.
        /// </summary>
        /// <param name="category">The category for the attribute.</param>
        /// <param name="checkId">The check identifier for the suppressed message.</param>
        public UnconditionalSuppressMessageAttribute(string category, string checkId)
        {
            Category = category;
            CheckId = checkId;
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the check id.
        /// </summary>
        public string CheckId { get; }

        /// <summary>
        /// Gets or sets the justification.
        /// </summary>
        public string? Justification { get; set; }

        /// <summary>
        /// Gets or sets the scope for which the attribute is applied.
        /// </summary>
        public string? Scope { get; set; }

        /// <summary>
        /// Gets or sets the target identifier on which the attribute is applied.
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// Gets or sets the message id.
        /// </summary>
        public string? MessageId { get; set; }
    }
}
#endif
