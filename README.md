![License](https://img.shields.io/github/license/ChrisPulman/TwinCATRx.svg) [![Build](https://github.com/ChrisPulman/TwinCATRx/actions/workflows/BuildOnly.yml/badge.svg)](https://github.com/ChrisPulman/TwinCATRx/actions/workflows/BuildOnly.yml) ![Nuget](https://img.shields.io/nuget/dt/CP.TwinCATRx?color=pink&style=plastic) [![NuGet](https://img.shields.io/nuget/v/CP.TwinCATRx.svg?style=plastic)](https://www.nuget.org/packages/CP.TwinCATRx)

---

# TwinCATRx

A reactive, cross-platform wrapper for Beckhoff TwinCAT ADS built on System.Reactive (Rx). It lets you observe PLC variables as IObservable<T>, write values, and work with structured tags using a HashTable-like API.

### Packages
- CP.TwinCATRx: main reactive client and extension APIs.
- CP.TwinCATRx.Core: shared helpers (settings, code generation, Rx extensions, ADS observables).

### Supported frameworks
- .NET Standard 2.0
- .NET 8
- .NET 9
- Windows-specific features (service monitoring) are enabled for net8.0-windows10.0.17763.0 and net9.0-windows10.0.17763.0.

### Install
```bash
# Main package
dotnet add package CP.TwinCATRx
# Optional low-level helpers (usually not required directly)
dotnet add package CP.TwinCATRx.Core
```

## 🚀 Quick Start

```csharp
using CP.TwinCatRx;
using CP.TwinCatRx.Core;

// Create client and settings
var client = new RxTcAdsClient();
var settings = new Settings { AdsAddress = "5.35.59.10.1.1", Port = 801, SettingsId = "Default" };

// Register tags to observe (notifications) and write
settings.AddNotification(".Tag1");         // structure
settings.AddNotification(".AString", arraySize: 80); // string length required for string
settings.AddNotification(".AInt");
settings.AddWriteVariable(".ArrInt", 11);  // arrays require a length

client.Connect(settings);

// Wait for PLC to be ready
client.InitializeComplete.Subscribe(_ =>
{
    // One-shot reads of simple types
    client.Read(".AString");
    client.Read(".AInt");

    // Write a value
    client.Write(".AInt", 42);
});

// Observe tag changes as streams
client.Observe<string>(".AString").Subscribe(v => Console.WriteLine($"AString: {v}"));
client.Observe<short>(".AInt").Subscribe(v => Console.WriteLine($"AInt: {v}"));

// Observe arrays (triggered by reads)
client.Observe<short[]>(".ArrInt").Subscribe(arr => Console.WriteLine($"ArrInt[{arr.Length}]: {string.Join(",", arr)}"));
```

## 📖 API Reference

IRxTcAdsClient API
- Code: IObservable<string[]> of generated code artifacts (internal diagnostics).
- InitializeComplete: IObservable<Unit> signaled when connected and initialized.
- DataReceived: IObservable<(string Variable, object? Data, string? Id)> of raw variable updates.
- ErrorReceived: IObservable<Exception> of client errors.
- OnWrite: IObservable<string?> of write results (e.g., "Success" or error text).
- ReadWriteHandleInfo: IDictionary<string, uint?> of handles for notification variables.
- WriteHandleInfo: IDictionary<string, (uint? Handle, int ArrayLength)> of handles for write variables.
- Settings: ISettings? used during Connect.
- IsPaused: bool indicating WriteValuesAsync throttling state.
- IsPausedObservable: IObservable<bool> of pause/resume state.
- Connect(ISettings settings): connect and initialize.
- Disconnect(): stop the client.
- Read(string variable, int? arrayLength = null, string? id = null): one-shot read for simple or array variables.
- Write(string variable, object value, string? id = null): write a value to a variable.
- Pause(TimeSpan time): temporarily pause WriteValuesAsync scheduling.

## Examples

### Observe variable updates
```csharp
client.Observe<bool>(".ABool").Subscribe(v => Console.WriteLine($"ABool: {v}"));
client.Observe<int>(".ADInt").Subscribe(v => Console.WriteLine($"ADInt: {v}"));

// With correlation id
client.Observe<int>(".AInt", id: "R1").Subscribe(v => Console.WriteLine($"AInt[R1]: {v}"));
```

### Read and write values
```csharp
// Simple types
client.Read(".ALReal");
client.Write(".ABool", true);

// Arrays
// For arrays registered via AddWriteVariable(".ArrInt", 11) you can read with the configured length
client.Read(".ArrInt");
// Or supply a length for arrays not registered as write variables
client.Read(".ArrInt", arrayLength: 11);
```

### Structured tags with HashTableRx

TwinCatRxExtensions provides helpers to work with structured PLC tags using CP.Collections.HashTableRx.
```csharp
// Create a live structure wrapper for a tag (structure or UDT)
var tag1 = client.CreateStruct(".Tag1");

// Wait until the first payload is received
await tag1!.StructureReady();

// Stream inner fields
tag1.Observe<bool>("ABool").Subscribe(v => Console.WriteLine($"Tag1.ABool: {v}"));
tag1.Observe<short>("AInt").Subscribe(v => Console.WriteLine($"Tag1.AInt: {v}"));

// Clone, set values, and write back atomically
var ok = tag1.WriteValues(ht =>
{
    var current = ht.Value<short>("AInt");
    ht.Value("AInt", (short)(current + 10));
    ht.Value("AString", $"Int Value {current + 10}");
});

// Async write with a pause window
var okAsync = await tag1.WriteValuesAsync(ht =>
{
    ht.Value("AInt", 100);
    ht.Value("AString", "Updated");
}, TimeSpan.FromMilliseconds(300));
```

### TwinCatRxExtensions (main) reference

- Observe<T>(this IRxTcAdsClient, string variable)
- Observe<T>(this IRxTcAdsClient, string variable, string id)
- CreateStruct(this IRxTcAdsClient, string variable): HashTableRx
- WriteValues(this HashTableRx, Action<HashTableRx>): bool
- WriteValuesAsync(this HashTableRx, Action<HashTableRx>, TimeSpan): Task<bool>
- StructureReady(this HashTableRx): IObservable<HashTableRx>
- CreateClone(this HashTableRx): HashTableRx

### Settings and configuration
```csharp
var settings = new Settings
{
    AdsAddress = "5.35.59.10.1.1",  // or leave null/empty to use local port-only Connect
    Port = 801,
    SettingsId = "Default"
};

// Notifications (observed tags)
settings.AddNotification(".Tag1");        // structure
settings.AddNotification(".AString", arraySize: 80); // strings require a size
settings.AddNotification(".AInt");

// Write variables (readable and writable)
settings.AddWriteVariable(".ArrInt", 11); // arrays require a length
```

## Core helpers (CP.TwinCATRx.Core)

### Reactive retry helpers
```csharp
using CP.TwinCatRx.Core;

// Retry forever
source.OnErrorRetry();

// Retry with handler
source.OnErrorRetry<SomeType, TimeoutException>(ex => Log(ex));

// Retry with delay and count
source.OnErrorRetry<SomeType, Exception>(ex => Log(ex), retryCount: 5, delay: TimeSpan.FromSeconds(1));
```

### ADS observables
```csharp
using CP.TwinCatRx.Core;
using TwinCAT.Ads;

var ads = new AdsClient();
ads.Connect(801);

// State changed events
ads.AdsStateChangedObserver().Subscribe(e => Console.WriteLine($"ADS state changed: {e.State}"));

// Polling observer for StateInfo
ads.AdsStateObserver().Subscribe(si => Console.WriteLine($"ADS: {si.AdsState}"));
```

## Advanced (dynamic code generation)
TwinCATRx can generate types for complex structures at runtime to simplify marshaling. This is handled internally, but you can hook into emitted content via IRxTcAdsClient.Code. Dynamic emit/reflective APIs carry AOT/trimming caveats and are annotated accordingly.

### Windows-only: service monitoring
Available on Windows-targeted TFMs.
```csharp
using CP.TwinCatRx;

ObservableServiceController.GetServices()
    .Where(s => s.DisplayName is "TwinCAT System Service" or "TwinCAT3 System Service")
    .Subscribe(s =>
    {
        Console.WriteLine($"{s.DisplayName}: {s.Status}");
        s.StatusObserver.Subscribe(st => Console.WriteLine($"Status: {st}"));
        if (s.Status != ServiceControllerStatus.Running) s.Start();
    });
```

### Error handling
```csharp
client.ErrorReceived.Subscribe(ex => Console.WriteLine($"Error: {ex}"));
client.OnWrite.Subscribe(msg => Console.WriteLine($"Write: {msg}"));
```

### Performance and AOT notes
- Case-insensitive maps are used to avoid repeated string allocations.
- Reads use O(1) reverse handle lookups.
- Library is trimmable; dynamic features are annotated for AOT.

### Limitations
- Arrays of string are not supported at this time (including arrays of string inside structures).


## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

---

**TwinCATRx** - Empowering Industrial Automation with Reactive Technology ⚡🏭
