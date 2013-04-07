#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace OpenRA.Network
{
	public class GameServer
	{
		public readonly int Id = 0;
		public readonly string Name = null;
		public readonly string Address = null;
		public readonly int State = 0;
		public readonly int Players = 0;
		public readonly string Map = null;
		public readonly string[] Mods = { };
		public readonly int TTL = 0;

		public Dictionary<string, string> UsefulMods
		{
			get
			{
				return Mods
					.Where(v => v.Contains('@'))
					.ToDictionary(v => v.Split('@')[0], v => v.Split('@')[1]);
			}
		}

		static bool AreVersionsCompatible(string a, string b)
		{
			if (Game.Settings.Debug.IgnoreVersionMismatch)
				return true;

			return a == b;
		}

		public bool CanJoin()
		{
			//"waiting for players"
			if (State != 1)
				return false;

			// Mods won't match if there are a different number
			if (Game.CurrentMods.Count != Mods.Count())
				return false;

			// Don't have the map locally
			if (!Game.modData.AvailableMaps.ContainsKey(Map))
				if (!Game.Settings.Game.AllowDownloading)
					return false;

			return CompatibleVersion();
		}

		public bool CompatibleVersion()
		{
			return UsefulMods.All(m => Game.CurrentMods.ContainsKey(m.Key)
				&& AreVersionsCompatible(m.Value, Game.CurrentMods[m.Key].Version));
		}

		public int Latency = -1;
		bool hasBeenPinged;
		public void Ping()
		{
			if (!hasBeenPinged)
			{
				hasBeenPinged = true;
				var pingSender = new Ping();
				pingSender.PingCompleted += new PingCompletedEventHandler(pongRecieved);
				AutoResetEvent waiter = new AutoResetEvent(false);
				pingSender.SendAsync(Address.Split(':')[0], waiter);
			}
		}

		void pongRecieved(object sender, PingCompletedEventArgs e)
		{
			if (e.Cancelled || e.Error != null)
				Latency = -1;
			else
			{
				PingReply pong = e.Reply;
				if (pong != null && pong.Status == IPStatus.Success)
					Latency = (int)pong.RoundtripTime;
				else
					Latency = -1;
			}
		}
	}
}
