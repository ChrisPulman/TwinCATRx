// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using CP.TwinCatRx.Core;
using CoreTwinCatRxExtensions = CP.TwinCatRx.Core.TwinCatRxExtensions;

namespace TwinCATRx.Tests.Core;

/// <summary>Exercises deterministic core extension branches.</summary>
public class CoreExtensionsDeterministicCoverageTests
{
    /// <summary>The number of C# files across the root and nested directories.</summary>
    private const int RootAndNestedFileCount = 2;

    /// <summary>The total number of created test files.</summary>
    private const int AllFileCount = 3;

    /// <summary>The value returned by the typed retry sequence.</summary>
    private const int SuccessfulValue = 7;

    /// <summary>The value returned by the untyped retry sequence.</summary>
    private const int UntypedSuccessfulValue = 11;

    /// <summary>The expected subscription count.</summary>
    private const int ExpectedAttemptCount = 2;

    /// <summary>Verifies all directory overloads, recursion, filtering, and de-duplication.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Directory_Overloads_Filter_Recurse_And_Deduplicate()
    {
        var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "TwinCATRx_Directory_" + Guid.NewGuid()));
        var nested = directory.CreateSubdirectory("nested");
        try
        {
            await WriteEmptyFileAsync(Path.Combine(directory.FullName, "root.cs"));
            await WriteEmptyFileAsync(Path.Combine(directory.FullName, "root.txt"));
            await WriteEmptyFileAsync(Path.Combine(nested.FullName, "nested.cs"));

            await TUnitAssert.That(directory.GetFilesWhere(_ => false)).IsEmpty();
            await TUnitAssert.That(directory.GetFilesWhere("*.cs", _ => true).Length).IsEqualTo(1);
            await TUnitAssert.That(directory.GetFilesWhere("*.cs", SearchOption.AllDirectories, _ => true).Length).IsEqualTo(RootAndNestedFileCount);
            await TUnitAssert.That(directory.GetFilesWhere(["*.cs", "*.*"], _ => true).Length).IsEqualTo(RootAndNestedFileCount);
            await TUnitAssert.That(directory.GetFilesWhere(["*.cs", "*.txt"], SearchOption.AllDirectories, _ => true).Length).IsEqualTo(AllFileCount);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    /// <summary>Verifies directory argument validation.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Directory_Overloads_Validate_Null_Arguments()
    {
        var directory = new DirectoryInfo(Path.GetTempPath());

        await TUnitAssert.That(() => DirectoryInfoExtensions.GetFilesWhere(null!, _ => true)).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => directory.GetFilesWhere((Func<FileInfo, bool>)null!)).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => directory.GetFilesWhere((string)null!, _ => true)).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => directory.GetFilesWhere((string[])null!, _ => true)).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => directory.GetFilesWhere(["*"], (Func<FileInfo, bool>)null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies retry overloads, retry limits, negative delay normalization, and validation.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Retry_Overloads_Cover_Success_Error_And_Guards()
    {
        var attempts = 0;
        var errors = 0;
        var eventuallySuccessful = Observable.Defer<int>(() =>
        {
            attempts++;
            return attempts == 1
                ? Observable.Throw<int>(new InvalidOperationException("retry"))
                : Observable.Return(SuccessfulValue);
        });

        var value = CoreTwinCatRxExtensions.OnErrorRetry<int, InvalidOperationException>(
            eventuallySuccessful,
            _ => errors++,
            retryCount: ExpectedAttemptCount,
            delay: TimeSpan.FromTicks(-1)).ToEnumerable().Single();

        await TUnitAssert.That(value).IsEqualTo(SuccessfulValue);
        await TUnitAssert.That(attempts).IsEqualTo(ExpectedAttemptCount);
        await TUnitAssert.That(errors).IsEqualTo(1);

        var terminal = CoreTwinCatRxExtensions.OnErrorRetry<int, InvalidOperationException>(
            Observable.Throw<int>(new InvalidOperationException("stop")),
            _ => errors++,
            retryCount: 1);
        await TUnitAssert.That(() => terminal.ToEnumerable().ToArray()).Throws<InvalidOperationException>();

        IObservable<int?> nullSource = null!;
        await TUnitAssert.That(() => CoreTwinCatRxExtensions.OnErrorRetry(nullSource)).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => CoreTwinCatRxExtensions.OnErrorRetry<int, InvalidOperationException>(Observable.Return(1), null!)).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => CoreTwinCatRxExtensions.OnErrorRetry<int, InvalidOperationException>(Observable.Return(1), _ => { }, ExpectedAttemptCount, TimeSpan.Zero, null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies the untyped retry overload resubscribes.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Untyped_Retry_Resubscribes_Until_Success()
    {
        var attempts = 0;
        var source = Observable.Defer<int>(() => ++attempts == 1
            ? Observable.Throw<int>(new InvalidOperationException("retry"))
            : Observable.Return(UntypedSuccessfulValue));

        var result = CoreTwinCatRxExtensions.OnErrorRetry(source).ToEnumerable().Single();

        await TUnitAssert.That(result).IsEqualTo(UntypedSuccessfulValue);
        await TUnitAssert.That(attempts).IsEqualTo(ExpectedAttemptCount);
    }

    /// <summary>Verifies null settings are ignored and existing assemblies load.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Settings_Null_Guards_And_Assembly_Load_Success_Are_Covered()
    {
        ISettings nullSettings = null!;
        CoreTwinCatRxExtensions.AddNotification(nullSettings, ".Ignored");
        CoreTwinCatRxExtensions.AddWriteVariable(nullSettings, ".Ignored");

        var sourcePath = typeof(Settings).Assembly.Location;
        var assemblyPath = Path.Combine(Path.GetTempPath(), "TwinCATRx_Core_" + Guid.NewGuid() + ".dll");
        try
        {
            await Task.Run(() => File.Copy(sourcePath, assemblyPath));
            var assembly = assemblyPath.AssemblyLoad();
            var resolvedType = assemblyPath.GetType(typeof(Settings).FullName!);

            await TUnitAssert.That(assembly).IsNotNull();
            await TUnitAssert.That(resolvedType).IsNotNull();
            await TUnitAssert.That(resolvedType!.Name).IsEqualTo(nameof(Settings));
        }
        finally
        {
            File.Delete(assemblyPath);
        }
    }

    /// <summary>Verifies internal node disposal recursively clears state and is idempotent.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task NodeEmulator_Disposes_Children_And_Is_Idempotent()
    {
        var nodeType = typeof(Settings).Assembly.GetType("CP.TwinCatRx.Core.NodeEmulator")!;
        var node = Activator.CreateInstance(nodeType)!;
        var child = new DisposableNode();
        var nodes = (HashSet<INodeEmulator>)nodeType.GetProperty("Nodes")!.GetValue(node)!;
        _ = nodes.Add(child);
        nodeType.GetProperty("Tag")!.SetValue(node, new object());

        _ = nodeType.GetMethod("Dispose")!.Invoke(node, null);
        _ = nodeType.GetMethod("Dispose")!.Invoke(node, null);

        await TUnitAssert.That(child.DisposeCount).IsEqualTo(1);
        await TUnitAssert.That(nodeType.GetProperty("Nodes")!.GetValue(node)).IsNull();
        await TUnitAssert.That(nodeType.GetProperty("Tag")!.GetValue(node)).IsNull();
    }

    /// <summary>Writes an empty test file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The write task.</returns>
    private static Task WriteEmptyFileAsync(string path)
    {
#if NET48
        return Task.Run(() => File.WriteAllText(path, string.Empty));
#else
        return File.WriteAllTextAsync(path, string.Empty);
#endif
    }

    /// <summary>A disposable node used to verify recursive disposal.</summary>
    private sealed class DisposableNode : INodeEmulator
    {
        /// <summary>Gets the number of dispose calls.</summary>
        public int DisposeCount { get; private set; }

        /// <inheritdoc/>
        public HashSet<INodeEmulator>? Nodes { get; } = [];

        /// <inheritdoc/>
        public object? Tag { get; set; }

        /// <inheritdoc/>
        public string Text { get; set; } = string.Empty;

        /// <inheritdoc/>
        public void Dispose() => DisposeCount++;
    }
}
