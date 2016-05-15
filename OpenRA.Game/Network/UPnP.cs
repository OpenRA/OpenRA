#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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
		static NatDevice natDevice;
		static Mapping mapping;

		public static IPAddress ExternalIP { get; private set; }

		public static async Task DiscoverNatDevices(int timeout)
		{
			NatDiscoverer.TraceSource.Switch.Level = SourceLevels.Verbose;
			var logChannel = Log.Channel("nat");
			NatDiscoverer.TraceSource.Listeners.Add(new TextWriterTraceListener(logChannel.Writer));

			var natDiscoverer = new NatDiscoverer();
			var token = new CancellationTokenSource(timeout);
			natDevice = await natDiscoverer.DiscoverDeviceAsync(PortMapper.Upnp, token);
			try
			{
				ExternalIP = await natDevice.GetExternalIPAsync();
			}
			catch (Exception e)
			{
				Console.WriteLine("Getting the external IP from NAT device failed: {0}", e.Message);
				Log.Write("nat", e.StackTrace);
			}
		}

		public static async Task ForwardPort(int listen, int external)
		{
			mapping = new Mapping(Protocol.Tcp, listen, external, "OpenRA");
			try
			{
				await natDevice.CreatePortMapAsync(mapping);
			}
			catch (Exception e)
			{
				Console.WriteLine("Port forwarding failed: {0}", e.Message);
				Log.Write("nat", e.StackTrace);
			}
		}

		public static async Task RemovePortForward()
		{
			try
			{
				await natDevice.DeletePortMapAsync(mapping);
			}
			catch (Exception e)
			{
				Console.WriteLine("Port removal failed: {0}", e.Message);
				Log.Write("nat", e.StackTrace);
			}
		}
	}
}