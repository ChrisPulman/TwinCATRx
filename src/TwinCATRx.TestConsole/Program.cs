// Copyright (c) Chris Pulman. All rights reserved.
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
            var settings = new Settings { AdsAddress = "1.1.1.1.1.1", Port = 851, SettingsId = "Default" };
            settings.Notifications.Add(new Notification { UpdateRate = 100, Variable = "Tag" });
            client.Connect(settings);

            // read and write client
            client.Observe<bool>("Tag").Subscribe(data =>
                        {
                        });
            client.Write("Tag", true);

            // Create hashtable to store data
            var ht = client.CreateHashTableRx("Tag");

            // read from hashtable as stream
            ht.Observe<bool>("Tag").Subscribe(value =>
            {
            });

            // read from hashtable as one time read
            var tag = ht.Value<bool>("Tag");
            Console.ReadLine();
        }
    }
}
