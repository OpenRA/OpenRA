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
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Open.Nat;

namespace OpenRA.Network
{
	public class UPnP
	{
		public static NatDevice NatDevice;

		static NatDiscoverer natDiscoverer;
		static Mapping mapping;
		public static IPAddress ExternalIP;

		public static async Task NatDiscovery()
		{
			if (Game.Settings.Server.VerboseNatDiscovery)
				NatDiscoverer.TraceSource.Switch.Level = SourceLevels.Verbose;

			natDiscoverer = new NatDiscoverer();
			var token = new CancellationTokenSource();
			NatDevice = await natDiscoverer.DiscoverDeviceAsync(PortMapper.Upnp, token);
			Log.Write("server", "NAT discovery started.");

			try
			{
				ExternalIP = NatDevice.GetExternalIPAsync().Result;
			}
			catch
			{
				Log.Write("server", "Failed to fetch the router's external IP.");
			}
		}

		public static async void ForwardPort()
		{
			mapping = new Mapping(Protocol.Tcp, Game.Settings.Server.ListenPort, Game.Settings.Server.ExternalPort, "OpenRA");
			await NatDevice.CreatePortMapAsync(mapping);
			Log.Write("server", "Creating port mapping: protocol = {0}, public = {1}, private = {2}, lifetime = {3} s",
				mapping.Protocol, mapping.PublicPort, mapping.PrivatePort, mapping.Lifetime);
		}

		public static async void RemovePortforward()
		{
			await NatDevice.DeletePortMapAsync(mapping);
		}
	}
}