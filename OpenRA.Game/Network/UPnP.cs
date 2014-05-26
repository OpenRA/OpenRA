#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
				// Mono.Nat never raises the DeviceLost event
				//NatUtility.DeviceLost += DeviceLost;
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

		public static void TryStoppingNatDiscovery()
		{
			Log.Write("server", "Stopping NAT discovery.");

			try
			{
				// Mono.Nat.StopDiscovery method never fails because it just
				// reset an ManualEvent. This try-catch statement could be removed.
				NatUtility.StopDiscovery();
			}
			catch (Exception e)
			{
				Log.Write("server", "Failed to stop NAT device discovery: {0}", e);
				Game.Settings.Server.NatDeviceAvailable = false;
				Game.Settings.Server.AllowPortForward = false;
			}
				
			// The discovered NAT device/service can be UpnpNatDevice or PmpNatDevice
			// OpenRA only supports Upnp because it uses NatDevice.GetAllMappings method
			// that is not supported by PMP (it throws a NotSupportedException)
			if (NatDevice == null || NatDevice.GetType().Name != "UpnpNatDevice")
			{
				Log.Write("server", "No NAT devices with UPnP enabled found within {0} ms deadline. Disabling automatic port forwarding.".F(Game.Settings.Server.NatDiscoveryTimeout));
				Game.Settings.Server.NatDeviceAvailable = false;
				Game.Settings.Server.AllowPortForward = false;
			}
		}

		public static void DeviceFound(object sender, DeviceEventArgs args)
		{
			if (args.Device == null) // ?? it should never happen
				return; 
			
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
		

		public static void ForwardPort()
		{
			try
			{
				// Here OpenRA is using Permanent portmapping.
				// Permanent portmappings never expire and given a program can finish unexpectly (because someone unplug the PC, for example)
				// This mapping remains opened. 
				// A different approach is to specify a Lifetime and use a Timer (or similar) to renew the mapping periodically
				// then, if RemovePortforward method is not invoked the NAT will remove it automatically.
				// In those cases when the NAT fails with Only OnlyPermanentLeasesSupported (errCode: 725)
				// you can retry without lifetime.
				var mapping = new Mapping(Protocol.Tcp, Game.Settings.Server.ExternalPort, Game.Settings.Server.ListenPort /*lifetime*/);
				//try{
				NatDevice.CreatePortMap(mapping);
				//}catch(MappingException e){
				//	if(e.ErrorCode == 725 /*OnlyPermanentLeasesSupported*/){
				//		mapping.Lifetime = 0;
				//		NatDevice.CreatePortMap(mapping);
				//	}
				//	throws;
				//}
				Log.Write("server", "Create port mapping: protocol={0}, public={1}, private={2}", mapping.Protocol, mapping.PublicPort, mapping.PrivatePort);
			}
			catch (Exception e)
			{
				Log.Write("server", "Can not forward ports via UPnP: {0}", e);
				Game.Settings.Server.AllowPortForward = false;
			}
		}
		
		public static void RemovePortforward()
		{
			try
			{
				var mapping = new Mapping(Protocol.Tcp, Game.Settings.Server.ExternalPort, Game.Settings.Server.ListenPort);
				NatDevice.DeletePortMap(mapping);
				Log.Write("server", "Remove port mapping: protocol={0}, public={1}, private={2}", mapping.Protocol, mapping.PublicPort, mapping.PrivatePort);
			}
			catch (Exception e)
			{
				Log.Write("server", "Can not remove UPnP portforwarding rules: {0}", e);
				Game.Settings.Server.AllowPortForward = false;
			}
		}
	}
}
