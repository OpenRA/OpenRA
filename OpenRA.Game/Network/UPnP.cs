#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Open.Nat;

namespace OpenRA.Network
{
	public class UPnP
	{
		static IEnumerable<NatDevice> natDevices;
		static Mapping mapping;

		public static IPAddress ExternalIP;

		public static async Task TryNatDiscovery()
		{
			if (Game.Settings.Server.DiscoverNatDevices == false)
				Game.Settings.Server.AllowPortForward = false;

			try
			{
				NatDiscoverer.TraceSource.Switch.Level = SourceLevels.Verbose;
				Log.AddChannel("nat", "nat.log");
				NatDiscoverer.TraceSource.Listeners.Add(new TextWriterTraceListener(Log.Channels["nat"].Writer));

				var natDiscoverer = new NatDiscoverer();
				var token = new CancellationTokenSource(Game.Settings.Server.NatDiscoveryTimeout);
				natDevices = await natDiscoverer.DiscoverDevicesAsync(PortMapper.Upnp, token);
				foreach (var natDevice in natDevices)
				{
					try
					{
						ExternalIP = natDevice.GetExternalIPAsync().Result;
					}
					catch { }
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("NAT discovery failed: {0}", e.Message);
				Log.Write("nat", e.StackTrace);
				Game.Settings.Server.AllowPortForward = false;
			}

			Game.Settings.Server.AllowPortForward = true;
		}

		public static async void ForwardPort()
		{
			mapping = new Mapping(Protocol.Tcp, Game.Settings.Server.ListenPort, Game.Settings.Server.ExternalPort, "OpenRA");
			foreach (var natDevice in natDevices)
			{
				try
				{
					await natDevice.CreatePortMapAsync(mapping);
				}
				catch (Exception e)
				{
					Console.WriteLine("Port forwarding failed: {0}", e.Message);
				}
			}
		}

		public static async void RemovePortforward()
		{
			foreach (var natDevice in natDevices)
			{
				try
				{
					await natDevice.DeletePortMapAsync(mapping);
				}
				catch (Exception e)
				{
					Console.WriteLine("Port removal failed: {0}", e.Message);
				}
			}
		}
	}
}