﻿// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.Collections;
using CP.TwinCatRx;
using CP.TwinCATRx.Core;

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
            settings.Notifications.Add(new Notification(100, ".Tag1"));

            settings.Notifications.Add(new Notification(100, ".AString", 80));
            settings.Notifications.Add(new Notification(100, ".ABool"));
            settings.Notifications.Add(new Notification(100, ".AByte"));
            settings.Notifications.Add(new Notification(100, ".AInt"));
            settings.Notifications.Add(new Notification(100, ".ADInt"));
            settings.Notifications.Add(new Notification(100, ".AReal"));
            settings.Notifications.Add(new Notification(100, ".ALReal"));

            // TODO: Find a way to write arrays of string
            ////settings.WriteVariables.Add(new WriteVariable(".ArrString"));
            settings.WriteVariables.Add(new WriteVariable(".ArrBool", 11));
            settings.WriteVariables.Add(new WriteVariable(".ArrByte", 11));
            settings.WriteVariables.Add(new WriteVariable(".ArrInt", 11));
            settings.WriteVariables.Add(new WriteVariable(".ArrDInt", 11));
            settings.WriteVariables.Add(new WriteVariable(".ArrReal", 11));
            settings.WriteVariables.Add(new WriteVariable(".ArrLReal", 11));
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
            var tag1 = client.CreateStruct(".Tag1", true);
            tag1.StructureReady().Subscribe(data =>
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
                data.Value("AInt", (short)(tag + 10));
                data.Value("AString", $"Int Value {tag + 10}");
                client.Write(".Tag1", data.GetStucture());
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
                        var tag = tag1.Value<short>("AInt");
                        tag1.Value("AInt", (short)(tag + 10));
                        tag1.Value("AString", $"Int Value {tag + 10}");
                        client.Write(".Tag1", tag1.GetStucture());
                        break;
                }
            }

            client.Dispose();
            tag1.Dispose();
        }
    }
}
