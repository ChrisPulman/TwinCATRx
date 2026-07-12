// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace TwinCATRx.Tests.Rx;

/// <summary>Exercises the generated lean and System.Reactive attribute surfaces.</summary>
public class GeneratedAttributeCoverageTests
{
    /// <summary>Stores the expected default notification cycle time.</summary>
    private const int DefaultCycleTime = 100;

    /// <summary>Stores the expected default array-size sentinel.</summary>
    private const int DefaultArraySize = -1;

    /// <summary>Stores the TwinCAT 3 ADS port used by the attribute probe.</summary>
    private const int TwinCat3Port = 851;

    /// <summary>Stores the direct-notification cycle time used by the probe.</summary>
    private const int DirectCycleTime = 25;

    /// <summary>Stores the direct-notification array size used by the probe.</summary>
    private const int DirectArraySize = 8;

    /// <summary>Stores the structured-notification cycle time used by the probe.</summary>
    private const int StructuredCycleTime = 50;

    /// <summary>Stores the structured-notification array size used by the probe.</summary>
    private const int StructuredArraySize = 4;

    /// <summary>Stores the write-only array size used by the probe.</summary>
    private const int WriteOnlyArraySize = 12;

    /// <summary>Stores the structured PLC address used by the probe.</summary>
    private const string StructureAddress = ".Structure";

    /// <summary>Stores the address property name.</summary>
    private const string AddressProperty = "Address";

    /// <summary>Stores the array-size property name.</summary>
    private const string ArraySizeProperty = "ArraySize";

    /// <summary>Stores the can-write property name.</summary>
    private const string CanWriteProperty = "CanWrite";

    /// <summary>Stores the cycle-time property name.</summary>
    private const string CycleTimeProperty = "CycleTime";

    /// <summary>Stores the identifier property name.</summary>
    private const string IdProperty = "Id";

    /// <summary>Stores the observable-name property name.</summary>
    private const string ObservableNameProperty = "ObservableName";

    /// <summary>Stores the write-address property name.</summary>
    private const string WriteAddressProperty = "WriteAddress";

    /// <summary>Verifies every generated attribute constructor and mutable property.</summary>
    /// <param name="attributeNamespace">The generated attribute namespace.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments("CP.TwinCatRx")]
    [Arguments("CP.TwinCatRx.Reactive")]
    public async Task Generated_Attributes_Expose_Expected_State(string attributeNamespace)
    {
        var assembly = typeof(GeneratedStreams).Assembly;

        await VerifyStreamAttributeAsync(assembly, attributeNamespace);
        await VerifyConnectionAttributeAsync(assembly, attributeNamespace);
        await VerifyDirectAttributeAsync(assembly, attributeNamespace);
        await VerifyStructuredAttributeAsync(assembly, attributeNamespace);
        await VerifyWriteOnlyAttributeAsync(assembly, attributeNamespace);
    }

    /// <summary>Verifies the generated stream attribute.</summary>
    /// <param name="assembly">The containing assembly.</param>
    /// <param name="attributeNamespace">The generated attribute namespace.</param>
    /// <returns>The test task.</returns>
    private static async Task VerifyStreamAttributeAsync(Assembly assembly, string attributeNamespace)
    {
        var attribute = CreateAttribute(assembly, attributeNamespace, nameof(TwinCatReactiveStreamAttribute), ".Value", typeof(int));
        SetProperty(attribute, IdProperty, "stream-id");
        SetProperty(attribute, "PropertyName", "Value");
        SetProperty(attribute, ObservableNameProperty, "ValueChanged");

        await TUnitAssert.That(GetProperty<string>(attribute, "Variable")).IsEqualTo(".Value");
        await TUnitAssert.That(GetProperty<Type>(attribute, "DataType")).IsEqualTo(typeof(int));
        await TUnitAssert.That(GetProperty<string>(attribute, IdProperty)).IsEqualTo("stream-id");
        await TUnitAssert.That(GetProperty<string>(attribute, "PropertyName")).IsEqualTo("Value");
        await TUnitAssert.That(GetProperty<string>(attribute, ObservableNameProperty)).IsEqualTo("ValueChanged");
    }

    /// <summary>Verifies the generated connection attribute.</summary>
    /// <param name="assembly">The containing assembly.</param>
    /// <param name="attributeNamespace">The generated attribute namespace.</param>
    /// <returns>The test task.</returns>
    private static async Task VerifyConnectionAttributeAsync(Assembly assembly, string attributeNamespace)
    {
        var attribute = CreateAttribute(assembly, attributeNamespace, nameof(TwinCatPlcConnectionAttribute), "1.2.3.4.5.6", TwinCat3Port);
        SetProperty(attribute, "SettingsId", "settings-id");

        await TUnitAssert.That(GetProperty<string>(attribute, "AdsAddress")).IsEqualTo("1.2.3.4.5.6");
        await TUnitAssert.That(GetProperty<int>(attribute, "Port")).IsEqualTo(TwinCat3Port);
        await TUnitAssert.That(GetProperty<string>(attribute, "SettingsId")).IsEqualTo("settings-id");
    }

    /// <summary>Verifies the generated direct-notification attribute.</summary>
    /// <param name="assembly">The containing assembly.</param>
    /// <param name="attributeNamespace">The generated attribute namespace.</param>
    /// <returns>The test task.</returns>
    private static async Task VerifyDirectAttributeAsync(Assembly assembly, string attributeNamespace)
    {
        var attribute = CreateAttribute(assembly, attributeNamespace, nameof(DirectNotificationAttribute), ".Direct");
        await VerifyNotificationDefaultsAsync(attribute);

        SetProperty(attribute, CycleTimeProperty, DirectCycleTime);
        SetProperty(attribute, ArraySizeProperty, DirectArraySize);
        SetProperty(attribute, IdProperty, "direct-id");
        SetProperty(attribute, ObservableNameProperty, "DirectChanged");
        SetProperty(attribute, CanWriteProperty, false);
        SetProperty(attribute, WriteAddressProperty, ".DirectWrite");

        await TUnitAssert.That(GetProperty<string>(attribute, AddressProperty)).IsEqualTo(".Direct");
        await TUnitAssert.That(GetProperty<int>(attribute, CycleTimeProperty)).IsEqualTo(DirectCycleTime);
        await TUnitAssert.That(GetProperty<int>(attribute, ArraySizeProperty)).IsEqualTo(DirectArraySize);
        await TUnitAssert.That(GetProperty<string>(attribute, IdProperty)).IsEqualTo("direct-id");
        await TUnitAssert.That(GetProperty<string>(attribute, ObservableNameProperty)).IsEqualTo("DirectChanged");
        await TUnitAssert.That(GetProperty<bool>(attribute, CanWriteProperty)).IsFalse();
        await TUnitAssert.That(GetProperty<string>(attribute, WriteAddressProperty)).IsEqualTo(".DirectWrite");
    }

    /// <summary>Verifies the generated structured-notification attribute overloads.</summary>
    /// <param name="assembly">The containing assembly.</param>
    /// <param name="attributeNamespace">The generated attribute namespace.</param>
    /// <returns>The test task.</returns>
    private static async Task VerifyStructuredAttributeAsync(Assembly assembly, string attributeNamespace)
    {
        var rootOnly = CreateAttribute(assembly, attributeNamespace, nameof(StructuredNotificationAttribute), StructureAddress);
        await VerifyNotificationDefaultsAsync(rootOnly);
        await TUnitAssert.That(GetProperty<string?>(rootOnly, "MemberAddress")).IsNull();

        var attribute = CreateAttribute(assembly, attributeNamespace, nameof(StructuredNotificationAttribute), StructureAddress, "Nested.Value");
        SetProperty(attribute, CycleTimeProperty, StructuredCycleTime);
        SetProperty(attribute, ArraySizeProperty, StructuredArraySize);
        SetProperty(attribute, IdProperty, "structure-id");
        SetProperty(attribute, ObservableNameProperty, "StructureChanged");
        SetProperty(attribute, CanWriteProperty, false);
        SetProperty(attribute, WriteAddressProperty, ".Structure.Nested.WriteValue");

        await TUnitAssert.That(GetProperty<string>(attribute, AddressProperty)).IsEqualTo(StructureAddress);
        await TUnitAssert.That(GetProperty<string>(attribute, "MemberAddress")).IsEqualTo("Nested.Value");
        await TUnitAssert.That(GetProperty<int>(attribute, CycleTimeProperty)).IsEqualTo(StructuredCycleTime);
        await TUnitAssert.That(GetProperty<int>(attribute, ArraySizeProperty)).IsEqualTo(StructuredArraySize);
        await TUnitAssert.That(GetProperty<string>(attribute, IdProperty)).IsEqualTo("structure-id");
        await TUnitAssert.That(GetProperty<string>(attribute, ObservableNameProperty)).IsEqualTo("StructureChanged");
        await TUnitAssert.That(GetProperty<bool>(attribute, CanWriteProperty)).IsFalse();
        await TUnitAssert.That(GetProperty<string>(attribute, WriteAddressProperty)).IsEqualTo(".Structure.Nested.WriteValue");
    }

    /// <summary>Verifies the generated write-only attribute.</summary>
    /// <param name="assembly">The containing assembly.</param>
    /// <param name="attributeNamespace">The generated attribute namespace.</param>
    /// <returns>The test task.</returns>
    private static async Task VerifyWriteOnlyAttributeAsync(Assembly assembly, string attributeNamespace)
    {
        var attribute = CreateAttribute(assembly, attributeNamespace, nameof(WriteOnlyAttribute), ".WriteOnly");
        await TUnitAssert.That(GetProperty<int>(attribute, ArraySizeProperty)).IsEqualTo(DefaultArraySize);
        await TUnitAssert.That(GetProperty<string?>(attribute, IdProperty)).IsNull();

        SetProperty(attribute, ArraySizeProperty, WriteOnlyArraySize);
        SetProperty(attribute, IdProperty, "write-id");

        await TUnitAssert.That(GetProperty<string>(attribute, AddressProperty)).IsEqualTo(".WriteOnly");
        await TUnitAssert.That(GetProperty<int>(attribute, ArraySizeProperty)).IsEqualTo(WriteOnlyArraySize);
        await TUnitAssert.That(GetProperty<string>(attribute, IdProperty)).IsEqualTo("write-id");
    }

    /// <summary>Verifies defaults common to notification attributes.</summary>
    /// <param name="attribute">The notification attribute.</param>
    /// <returns>The test task.</returns>
    private static async Task VerifyNotificationDefaultsAsync(object attribute)
    {
        await TUnitAssert.That(GetProperty<int>(attribute, CycleTimeProperty)).IsEqualTo(DefaultCycleTime);
        await TUnitAssert.That(GetProperty<int>(attribute, ArraySizeProperty)).IsEqualTo(DefaultArraySize);
        await TUnitAssert.That(GetProperty<bool>(attribute, CanWriteProperty)).IsTrue();
        await TUnitAssert.That(GetProperty<string?>(attribute, IdProperty)).IsNull();
        await TUnitAssert.That(GetProperty<string?>(attribute, ObservableNameProperty)).IsNull();
        await TUnitAssert.That(GetProperty<string?>(attribute, WriteAddressProperty)).IsNull();
    }

    /// <summary>Creates a generated attribute instance.</summary>
    /// <param name="assembly">The containing assembly.</param>
    /// <param name="attributeNamespace">The generated attribute namespace.</param>
    /// <param name="typeName">The attribute type name.</param>
    /// <param name="arguments">The constructor arguments.</param>
    /// <returns>The generated attribute.</returns>
    private static object CreateAttribute(Assembly assembly, string attributeNamespace, string typeName, params object[] arguments)
    {
        var type = assembly.GetType(attributeNamespace + "." + typeName, throwOnError: true)!;
        return Activator.CreateInstance(type, arguments)!;
    }

    /// <summary>Gets a generated attribute property.</summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="attribute">The generated attribute.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The property value.</returns>
    private static T GetProperty<T>(object attribute, string propertyName) =>
        (T)attribute.GetType().GetProperty(propertyName)!.GetValue(attribute)!;

    /// <summary>Sets a generated attribute property.</summary>
    /// <param name="attribute">The generated attribute.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    private static void SetProperty(object attribute, string propertyName, object value) =>
        attribute.GetType().GetProperty(propertyName)!.SetValue(attribute, value);
}
