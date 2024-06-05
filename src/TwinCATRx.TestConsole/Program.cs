// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.Collections;
using CP.TwinCatRx;
using CP.TwinCatRx.Core;

namespace TwinCATRx.TestConsole
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
            // Create Client
            var client = new RxTcAdsClient();
            var settings = new Settings { AdsAddress = "5.35.59.10.1.1", Port = 801, SettingsId = "Default" };
            settings.AddNotification(".Tag1");
            settings.AddNotification(".Tag3");
            settings.AddNotification(".AString", arraySize: 80);
            settings.AddNotification(".ABool");
            settings.AddNotification(".AByte");
            settings.AddNotification(".AInt");
            settings.AddNotification(".ADInt");
            settings.AddNotification(".AReal");
            settings.AddNotification(".ALReal");

            // TODO: Find a way to read/write arrays of string
            ////settings.WriteVariables.Add(new WriteVariable(".ArrString"));
            settings.AddWriteVariable(".ArrBool", 11);
            settings.AddWriteVariable(".ArrByte", 11);
            settings.AddWriteVariable(".ArrInt", 11);
            settings.AddWriteVariable(".ArrDInt", 11);
            settings.AddWriteVariable(".ArrReal", 11);
            settings.AddWriteVariable(".ArrLReal", 11);
            client.Connect(settings);

            // read and write client
            client.Observe<string>(".AString").Subscribe(data => Console.WriteLine(data));
            client.Observe<bool>(".ABool").Subscribe(data => Console.WriteLine(data));
            client.Observe<byte>(".AByte").Subscribe(data => Console.WriteLine(data));
            client.Observe<short>(".AInt").Subscribe(data => Console.WriteLine(data));
            client.Observe<int>(".ADInt").Subscribe(data => Console.WriteLine(data));
            client.Observe<float>(".AReal").Subscribe(data => Console.WriteLine(data));
            client.Observe<double>(".ALReal").Subscribe(data => Console.WriteLine(data));
            client.OnWrite.Subscribe(data => Console.WriteLine(data));
            ////client.Observe<string[]>(".ArrString").Subscribe(data =>
            ////{
            ////    Console.WriteLine(data);
            ////});
            client.Observe<bool[]>(".ArrBool").Subscribe(data => Console.WriteLine(data));
            client.Observe<byte[]>(".ArrByte").Subscribe(data => Console.WriteLine(data));
            client.Observe<short[]>(".ArrInt").Subscribe(data => Console.WriteLine(data));
            client.Observe<int[]>(".ArrDInt").Subscribe(data => Console.WriteLine(data));
            client.Observe<float[]>(".ArrReal").Subscribe(data => Console.WriteLine(data));
            client.Observe<double[]>(".ArrLReal").Subscribe(data => Console.WriteLine(data));

            Console.ReadLine();
            //// ---- TESTED AND WORKING ----
            ////client.Read(".AString");
            ////client.Read(".ABool");
            ////client.Read(".AByte");
            ////client.Read(".AInt");
            ////client.Read(".ADInt");
            ////client.Read(".AReal");
            ////client.Read(".ALReal");

            // Issue reading arrays of string
            ////client.Read(".ArrString");
            client.Read(".ArrBool");
            client.Read(".ArrByte");
            client.Read(".ArrInt");
            client.Read(".ArrDInt");
            client.Read(".ArrReal");
            client.Read(".ArrLReal");
            Console.ReadLine();

            client.Write(".ABool", true);
            client.Read(".ABool");
            Console.ReadLine();

            // Create structure to store data
            var tag1 = client.CreateStruct(".Tag1");
            tag1?.StructureReady().Subscribe(data =>
            {
                Console.WriteLine("Structure ready");

                // read from structure as stream
                data.Observe<bool>("ABool").Subscribe(value => Console.WriteLine(value));

                data.Observe<short>("AInt").Subscribe(value => Console.WriteLine(value));

                data.Observe<string>("AString").Subscribe(value => Console.WriteLine(value));

                // read from structure as one time read from the first level.
                var tag = data.Value<short>("AInt");
                Console.WriteLine(tag);
                Console.WriteLine("Press any key to write structure");
                Console.ReadLine();

                data.WriteValues(ht =>
                {
                    ht.Value("AInt", (short)(tag + 10));
                    ht.Value("AString", $"Int Value {tag + 10}");
                });
            });

            var tag3 = client.CreateStruct(".Tag3");
            tag3?.StructureReady().Subscribe(data =>
            {
                Console.WriteLine("Structure ready");
                data.Observe<bool[]>("ArrBool").Subscribe(value => Console.WriteLine(value));
                data.Observe<byte[]>("ArrByte").Subscribe(value => Console.WriteLine(value));
                data.Observe<short[]>("ArrInt").Subscribe(value => Console.WriteLine(value));
                data.Observe<int[]>("ArrDInt").Subscribe(value => Console.WriteLine(value));
                data.Observe<float[]>("ArrReal").Subscribe(value => Console.WriteLine(value));
                data.Observe<double[]>("ArrLReal").Subscribe(value => Console.WriteLine(value));
            });
            var exit = false;
            while (!exit)
            {
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        exit = true;
                        break;
                    case ConsoleKey.W:
                        var tag = tag1?.Value<short>("AInt");
                        tag1?.WriteValues(ht =>
                        {
                            ht.Value("AInt", (short)(tag! + 10));
                            ht.Value("AString", $"Int Value {tag + 10}");
                        });

                        break;
                }
            }

            client.Dispose();
            tag1?.Dispose();
            tag3?.Dispose();
        }
    }
}
