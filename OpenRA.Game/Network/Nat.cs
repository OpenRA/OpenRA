#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Threading;
using Mono.Nat;

namespace OpenRA.Network
{
	public enum NatStatus { Enabled, Disabled, NotSupported }

	public class Nat
	{
		public static NatStatus Status => NatUtility.IsSearching ? natDevice != null ? NatStatus.Enabled : NatStatus.NotSupported : NatStatus.Disabled;

		static Mapping mapping;
		static INatDevice natDevice;
		static bool initialized;

		public static void Initialize()
		{
			if (initialized)
				return;

			if (Game.Settings.Server.DiscoverNatDevices)
			{
				NatUtility.DeviceFound += DeviceFound;
				NatUtility.StartDiscovery();
			}

			initialized = true;
		}

		static readonly SemaphoreSlim Locker = new SemaphoreSlim(1, 1);

		static async void DeviceFound(object sender, DeviceEventArgs args)
		{
			await Locker.WaitAsync();
			try
			{
				// Only interact with one at a time. Some support both UPnP and NAT-PMP.
				natDevice = args.Device;

				Log.Write("nat", "Device found: {0}", natDevice.DeviceEndpoint);
				Log.Write("nat", "Type: {0}", natDevice.NatProtocol);
			}
			finally
			{
				Locker.Release();
			}
		}

		public static bool TryForwardPort(int listen, int external)
		{
			if (natDevice == null)
				return false;

			var lifetime = Game.Settings.Server.NatPortMappingLifetime;
			mapping = new Mapping(Protocol.Tcp, listen, external, lifetime, "OpenRA");
			try
			{
				natDevice.CreatePortMap(mapping);
			}
			catch (Exception e)
			{
				Console.WriteLine("Port forwarding failed: {0}", e.Message);
				Log.Write("nat", e.StackTrace);
				return false;
			}

			return true;
		}

		public static bool TryRemovePortForward()
		{
			if (natDevice == null)
				return false;

			try
			{
				natDevice.DeletePortMap(mapping);
			}
			catch (Exception e)
			{
				Console.WriteLine("Port removal failed: {0}", e.Message);
				Log.Write("nat", e.StackTrace);
				return false;
			}

			return true;
		}
	}
}
