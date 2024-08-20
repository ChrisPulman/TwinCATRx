// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reactive.Disposables;

internal static class DisposableMixins
{
    /// <summary>
    /// Ensures the provided disposable is disposed with the specified <see cref="CompositeDisposable"/>.
    /// </summary>
    /// <typeparam name="T">The type of the disposable.</typeparam>
    /// <param name="this">The disposable.</param>
    /// <param name="compositeDisposable">
    /// The <see cref="CompositeDisposable"/> to which <paramref name="this"/> will be added.
    /// </param>
    /// <returns>The Composite Disposable.</returns>
    public static T? DisposeWith<T>(this T? @this, CompositeDisposable compositeDisposable)
        where T : IDisposable
    {
        compositeDisposable.Add(@this!);
        return @this;
    }
}
