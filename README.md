# TwinCATRx

![License](https://img.shields.io/github/license/ChrisPulman/TwinCATRx.svg) [![Build](https://github.com/ChrisPulman/TwinCATRx/actions/workflows/BuildOnly.yml/badge.svg)](https://github.com/ChrisPulman/TwinCATRx/actions/workflows/BuildOnly.yml) ![Nuget](https://img.shields.io/nuget/dt/CP.TwinCATRx?color=pink&style=plastic) [![NuGet](https://img.shields.io/nuget/v/CP.TwinCATRx.svg?style=plastic)](https://www.nuget.org/packages/CP.TwinCATRx)

A Reactive implementation of TwinCAT ADS

This is a reactive implementation of TwinCAT ADS. It is based on the TwinCAT ADS library from Beckhoff. 
It is a wrapper around the TwinCAT ADS library that allows you to use the TwinCAT ADS library in a reactive way. 
It is based on the Reactive Extensions (Rx) library.

Currently it does not support the following features:
- Arrays of string
- Arrays of string in structures

```c#
    // Create Client
    var client = new RxTcAdsClient();
    var settings = new Settings { AdsAddress = "5.35.59.10.1.1", Port = 801, SettingsId = "Default" };

    // Add notification variables
    // Structures
    settings.AddNotification(".Tag1");
    settings.AddNotification(".Tag2");
    settings.AddNotification(".Tag3");
    settings.AddNotification(".AString", arraySize: 80);
    settings.AddNotification(".ABool");
    settings.AddNotification(".AByte");
    settings.AddNotification(".AInt");
    settings.AddNotification(".ADInt");
    settings.AddNotification(".AReal");
    settings.AddNotification(".ALReal");

    // Add Write variables, these can be read too using client.Read("TagName")
    // NOT SUPPORTED: arrays of string
    ////settings.AddWriteVariable(".ArrString", 11));
    settings.AddWriteVariable(".ArrBool", 11);
    settings.AddWriteVariable(".ArrByte", 11);
    settings.AddWriteVariable(".ArrInt", 11);
    settings.AddWriteVariable(".ArrDInt", 11);
    settings.AddWriteVariable(".ArrReal", 11);
    settings.AddWriteVariable(".ArrLReal", 11);
    client.Connect(settings);

    // Observe notification tags simple types
    client.Observe<string>(".AString").Subscribe(data => Console.WriteLine(data));
    client.Observe<bool>(".ABool").Subscribe(data => Console.WriteLine(data));
    client.Observe<byte>(".AByte").Subscribe(data => Console.WriteLine(data));
    client.Observe<short>(".AInt").Subscribe(data => Console.WriteLine(data));
    client.Observe<int>(".ADInt").Subscribe(data => Console.WriteLine(data));
    client.Observe<float>(".AReal").Subscribe(data => Console.WriteLine(data));
    client.Observe<double>(".ALReal").Subscribe(data => Console.WriteLine(data));

    // Observe Write variables these will execute when tag is read.
    client.Observe<bool[]>(".ArrBool").Subscribe(data => Console.WriteLine(data));
    client.Observe<byte[]>(".ArrByte").Subscribe(data => Console.WriteLine(data));
    client.Observe<short[]>(".ArrInt").Subscribe(data => Console.WriteLine(data));
    client.Observe<int[]>(".ArrDInt").Subscribe(data => Console.WriteLine(data));
    client.Observe<float[]>(".ArrReal").Subscribe(data => Console.WriteLine(data));
    client.Observe<double[]>(".ArrLReal").Subscribe(data => Console.WriteLine(data));

    // Ensure the client is initialized before reading or writing
    client.InitializeComplete.Subscribe(() =>
    {
        // Read tags of a simple type
        client.Read(".AString");
        client.Read(".ABool");
        client.Read(".AByte");
        client.Read(".AInt");
        client.Read(".ADInt");
        client.Read(".AReal");
        client.Read(".ALReal");

        // Write a value
        client.Write(".ABool", true);
    });

    // Create structure to store data
    var tag1 = client.CreateStruct(".Tag1", true);
    tag1.StructureReady().Subscribe(async data =>
    {
        // read from structure as stream
        data.Observe<bool>("ABool").Subscribe(value => Console.WriteLine(value));
        data.Observe<short>("AInt").Subscribe(value => Console.WriteLine(value));
        data.Observe<string>("AString").Subscribe(value => Console.WriteLine(value));

        // read from structure as one time read from the first level.
        var tag = data.Value<short>("AInt");

        data.WriteValues(ht =>
        {
            // write values to structure
            ht.Value("AInt", (short)(tag + 10));
            ht.Value("AString", $"Int Value {tag + 10}");
            
            // Values are written from the structure to the PLC upon return.
        });

        await data.WriteValuesAsync(ht =>
        {
            // write values to structure
            ht.Value("AInt", (short)(tag + 10));
            ht.Value("AString", $"Int Value {tag + 10}");
            
            // Values are written from the structure to the PLC upon return, 
            // future usage of WriteValuesAsync will be delayed by the timespan specified with each call.
        }, TimeSpan.FromMilliseconds(300))
    });
```
