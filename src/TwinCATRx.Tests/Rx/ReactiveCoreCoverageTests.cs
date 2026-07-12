// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Reflection;
using System.Text;
using ReactiveCodeGenerator = CP.TwinCatRx.Core.Reactive.CodeGenerator;
using ReactiveCoreExtensions = CP.TwinCatRx.Core.Reactive.TwinCatRxExtensions;
using ReactiveDirectoryExtensions = CP.TwinCatRx.Core.Reactive.DirectoryInfoExtensions;
using ReactiveNode = CP.TwinCatRx.Core.Reactive.INodeEmulator;
using ReactiveSettings = CP.TwinCatRx.Core.Reactive.Settings;
using ReactiveSimpleTypeException = CP.TwinCatRx.Core.Reactive.SimpleTypeException;
using ReactiveUnsupportedTypeException = CP.TwinCatRx.Core.Reactive.UnsuportedTypeException;

namespace TwinCATRx.Tests.Rx;

/// <summary>Exercises linked source in the Reactive core assembly.</summary>
public class ReactiveCoreCoverageTests
{
    /// <summary>The expected successful retry value.</summary>
    private const int RetryValue = 42;

    /// <summary>The notification cycle time.</summary>
    private const int CycleTime = 25;

    /// <summary>The notification array size and successful retry attempt.</summary>
    private const int Three = 3;

    /// <summary>The write array size.</summary>
    private const int WriteArraySize = 4;

    /// <summary>The expected count for recursive file searches and retry errors.</summary>
    private const int Two = 2;

    /// <summary>Verifies Reactive settings registration and defaults.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Settings_Defaults_And_Registrations_Work()
    {
        var settings = new ReactiveSettings();
        ReactiveCoreExtensions.AddNotification(settings, ".Input", cycleTime: CycleTime, arraySize: Three);
        ReactiveCoreExtensions.AddWriteVariable(settings, ".Output", arraySize: WriteArraySize);
        var defaults = new ReactiveSettings().Defaults<ReactiveSettings>();

        await TUnitAssert.That(settings.Notifications.Count).IsEqualTo(1);
        await TUnitAssert.That(settings.Notifications[0].Variable).IsEqualTo(".Input");
        await TUnitAssert.That(settings.Notifications[0].UpdateRate).IsEqualTo(CycleTime);
        await TUnitAssert.That(settings.Notifications[0].ArraySize).IsEqualTo(Three);
        await TUnitAssert.That(settings.WriteVariables.Count).IsEqualTo(1);
        await TUnitAssert.That(settings.WriteVariables[0].Variable).IsEqualTo(".Output");
        await TUnitAssert.That(settings.WriteVariables[0].ArraySize).IsEqualTo(WriteArraySize);
        await TUnitAssert.That(defaults.SettingsId).IsEqualTo("Defaults");
        await TUnitAssert.That(defaults.Notifications).IsNotEmpty();
        await TUnitAssert.That(defaults.WriteVariables).IsNotEmpty();
    }

    /// <summary>Verifies Reactive retry overloads with deterministic immediate scheduling.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task OnErrorRetry_Uses_System_Reactive_Scheduler_And_Limits_Retries()
    {
        var attempts = 0;
        var errors = 0;
        var sequence = Observable.Defer(() =>
        {
            attempts++;
            return attempts < Three
                ? Observable.Throw<int>(new InvalidOperationException("retry"))
                : Observable.Return(RetryValue);
        });

        var retried = ReactiveCoreExtensions.OnErrorRetry<int, InvalidOperationException>(
            sequence,
            _ => errors++,
            retryCount: Three,
            delay: TimeSpan.Zero,
            delaySequencer: ImmediateScheduler.Instance);
        var result = System.Reactive.Linq.Observable.ToEnumerable(retried).Single();

        await TUnitAssert.That(result).IsEqualTo(RetryValue);
        await TUnitAssert.That(attempts).IsEqualTo(Three);
        await TUnitAssert.That(errors).IsEqualTo(Two);
    }

    /// <summary>Verifies file filtering overloads in the Reactive assembly.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Directory_Filters_Exercise_All_Overloads()
    {
        var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "TwinCATRxReactive_" + Guid.NewGuid()));
        directory.Create();
        try
        {
            await WriteEmptyFileAsync(Path.Combine(directory.FullName, "a.cs"));
            await WriteEmptyFileAsync(Path.Combine(directory.FullName, "b.txt"));
            var nested = directory.CreateSubdirectory("nested");
            await WriteEmptyFileAsync(Path.Combine(nested.FullName, "c.cs"));

            var predicate = ReactiveDirectoryExtensions.GetFilesWhere(directory, file => file.Extension == ".cs");
            var pattern = ReactiveDirectoryExtensions.GetFilesWhere(directory, "*.txt", _ => true);
            var recursive = ReactiveDirectoryExtensions.GetFilesWhere(directory, "*.cs", SearchOption.AllDirectories, _ => true);
            var patterns = ReactiveDirectoryExtensions.GetFilesWhere(directory, ["*.cs", "*.txt"], _ => true);
            var recursivePatterns = ReactiveDirectoryExtensions.GetFilesWhere(directory, ["*.cs"], SearchOption.AllDirectories, _ => true);

            await TUnitAssert.That(predicate.Length).IsEqualTo(1);
            await TUnitAssert.That(pattern.Length).IsEqualTo(1);
            await TUnitAssert.That(recursive.Length).IsEqualTo(Two);
            await TUnitAssert.That(patterns.Length).IsEqualTo(Two);
            await TUnitAssert.That(recursivePatterns.Length).IsEqualTo(Two);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    /// <summary>Verifies Reactive core assembly/type helpers and exception state.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Assembly_Helpers_And_Exceptions_Cover_Failure_Paths()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
        var inner = new InvalidOperationException("inner");

        await TUnitAssert.That(ReactiveCoreExtensions.AssemblyLoad(missing)).IsNull();
        await TUnitAssert.That(ReactiveCoreExtensions.GetType(missing, "Missing.Type")).IsNull();
        await TUnitAssert.That(new ReactiveSimpleTypeException()).IsNotNull();
        await TUnitAssert.That(new ReactiveSimpleTypeException("simple").Message).IsEqualTo("simple");
        await TUnitAssert.That(new ReactiveSimpleTypeException("wrapped", inner).InnerException).IsSameReferenceAs(inner);
        await TUnitAssert.That(new ReactiveUnsupportedTypeException()).IsNotNull();
        await TUnitAssert.That(new ReactiveUnsupportedTypeException("unsupported").Message).IsEqualTo("unsupported");
        await TUnitAssert.That(new ReactiveUnsupportedTypeException("wrapped", inner).InnerException).IsSameReferenceAs(inner);
    }

    /// <summary>Verifies Reactive code-generator mappings and simple-node behavior.</summary>
    /// <param name="plcType">The PLC type text.</param>
    /// <param name="expected">The expected C# type prefix.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments("BOOL", "System.Boolean")]
    [Arguments("DINT", "System.Int32")]
    [Arguments("ARRAY [0..2] OF BYTE", "System.Byte[]")]
    [Arguments(null, "")]
    public async Task CodeGenerator_Maps_Types_And_Rejects_Simple_Nodes(string? plcType, string expected)
    {
        var mapped = ReactiveCodeGenerator.PLCToCSharpTypeConverter(plcType);
        var generator = new ReactiveCodeGenerator();
        var node = new FakeReactiveNode();
        var method = typeof(ReactiveCodeGenerator).GetMethod("CreateCsharpCodeFile", BindingFlags.Instance | BindingFlags.NonPublic)!;

        await TUnitAssert.That(mapped.StartsWith(expected, StringComparison.Ordinal)).IsTrue();
        await TUnitAssert.That(generator.CreateCSharpCodeString(node)).IsEqualTo(string.Empty);
        var exception = await TUnitAssert.That(() => method.Invoke(generator, [new StringBuilder(), node, "TwinCATRx", false])).Throws<TargetInvocationException>();
        await TUnitAssert.That(exception!.InnerException).IsTypeOf<ReactiveSimpleTypeException>();
    }

    /// <summary>Verifies internal language and node helpers in the Reactive assembly.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Internal_Language_And_Node_Helpers_Work_Through_Reflection()
    {
        var assembly = typeof(ReactiveSettings).Assembly;
        var languageType = assembly.GetType("CP.TwinCatRx.Core.Reactive.CSharpLanguage")!;
        var language = Activator.CreateInstance(languageType, nonPublic: true)!;
        var tree = languageType.GetMethod("ParseText")!.Invoke(language, ["class ReactiveSample { }", Microsoft.CodeAnalysis.SourceCodeKind.Regular]);
        var compilation = languageType.GetMethod("CreateLibraryCompilation")!.Invoke(language, ["ReactiveCoverage", true]);
        var nodeType = assembly.GetType("CP.TwinCatRx.Core.Reactive.NodeEmulator")!;
        var node = Activator.CreateInstance(nodeType)!;
        nodeType.GetProperty("Tag")!.SetValue(node, "tag");
        _ = nodeType.GetMethod("Dispose")!.Invoke(node, null);

        await TUnitAssert.That(tree).IsNotNull();
        await TUnitAssert.That(compilation).IsNotNull();
        await TUnitAssert.That(nodeType.GetProperty("Nodes")!.GetValue(node)).IsNull();
        await TUnitAssert.That(nodeType.GetProperty("Tag")!.GetValue(node)).IsNull();
    }

    /// <summary>Writes an empty deterministic test file.</summary>
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

    /// <summary>Minimal node used for simple-type generator paths.</summary>
    private sealed class FakeReactiveNode : ReactiveNode
    {
        /// <inheritdoc/>
        public HashSet<ReactiveNode>? Nodes { get; } = [];

        /// <inheritdoc/>
        public object? Tag { get; set; }

        /// <inheritdoc/>
        public string Text { get; set; } = string.Empty;

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
