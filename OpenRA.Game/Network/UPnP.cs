#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;

using Mono.Nat;

namespace OpenRA.Network
{
	public static class UPnP
	{
		public static INatDevice NatDevice;

		public static void TryNatDiscovery()
		{
			try
			{
				NatUtility.Logger = Log.Channels["server"].Writer;
				NatUtility.Verbose = Game.Settings.Server.VerboseNatDiscovery;
				NatUtility.DeviceFound += DeviceFound;
				Game.Settings.Server.NatDeviceAvailable = false;
				NatUtility.StartDiscovery();
				Log.Write("server", "NAT discovery started.");
			}
			catch (Exception e)
			{
				Log.Write("server", "Can't discover UPnP-enabled device: {0}", e);
				Game.Settings.Server.NatDeviceAvailable = false;
				Game.Settings.Server.AllowPortForward = false;
			}
		}

		public static void StoppingNatDiscovery()
		{
			Log.Write("server", "Stopping NAT discovery.");
			NatUtility.StopDiscovery();

			if (NatDevice == null || NatDevice.GetType() != typeof(Mono.Nat.Upnp.UpnpNatDevice))
			{
				Log.Write("server", "No NAT devices with UPnP enabled found within {0} ms deadline. Disabling automatic port forwarding.".F(Game.Settings.Server.NatDiscoveryTimeout));
				Game.Settings.Server.NatDeviceAvailable = false;
				Game.Settings.Server.AllowPortForward = false;
			}
		}

		public static void DeviceFound(object sender, DeviceEventArgs args)
		{
			Log.Write("server", "NAT device discovered.");

			Game.Settings.Server.NatDeviceAvailable = true;
			Game.Settings.Server.AllowPortForward = true;

			try
			{
				NatDevice = args.Device;
				Log.Write("server", "Type: {0}", NatDevice.GetType());
				Log.Write("server", "Your external IP is: {0}", NatDevice.GetExternalIP());

				foreach (var mp in NatDevice.GetAllMappings())
					Log.Write("server", "Existing port mapping: protocol={0}, public={1}, private={2}",
						mp.Protocol, mp.PublicPort, mp.PrivatePort);
			}
			catch (Exception e)
			{
				Log.Write("server", "Can't fetch information from NAT device: {0}", e);

				Game.Settings.Server.NatDeviceAvailable = false;
				Game.Settings.Server.AllowPortForward = false;
			}
		}

		public static void ForwardPort(int lifetime)
		{
			try
			{
				var mapping = new Mapping(Protocol.Tcp, Game.Settings.Server.ExternalPort, Game.Settings.Server.ListenPort, lifetime);
				NatDevice.CreatePortMap(mapping);
				Log.Write("server", "Create port mapping: protocol = {0}, public = {1}, private = {2}, lifetime = {3} s",
					mapping.Protocol, mapping.PublicPort, mapping.PrivatePort, mapping.Lifetime);
			}
			catch (MappingException e)
			{
				if (e.ErrorCode == 725 && lifetime != 0)
				{
					Log.Write("server", "NAT device answered with OnlyPermanentLeasesSupported. Retrying...");
					ForwardPort(0);
				}
				else
				{
					Log.Write("server", "Can not forward ports via UPnP: {0}", e);
					Game.Settings.Server.AllowPortForward = false;
				}
			}
		}

		public static void RemovePortforward()
		{
			try
			{
				var mapping = new Mapping(Protocol.Tcp, Game.Settings.Server.ExternalPort, Game.Settings.Server.ListenPort);
				NatDevice.DeletePortMap(mapping);
				Log.Write("server", "Remove port mapping: protocol = {0}, public = {1}, private = {2}, expiration = {3}",
					mapping.Protocol, mapping.PublicPort, mapping.PrivatePort, mapping.Expiration);
			}
			catch (Exception e)
			{
				Log.Write("server", "Can not remove UPnP portforwarding rules: {0}", e);
				Game.Settings.Server.AllowPortForward = false;
			}
		}
	}
}