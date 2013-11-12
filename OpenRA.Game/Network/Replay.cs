#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;

namespace OpenRA.Network
{
	/* HACK: a maze of twisty little hacks... */
	public class Replay
	{
		public readonly string Filename;
		public readonly int Duration;
		public readonly Session LobbyInfo;

		public Replay(string filename)
		{
			Filename = filename;
			var lastFrame = 0;
			var hasSeenGameStart = false;
			var lobbyInfo = null as Session;

			using (var conn = new ReplayConnection(filename))
				conn.Receive((client, packet) =>
					{
						var frame = BitConverter.ToInt32(packet, 0);
						if (packet.Length == 5 && packet[4] == 0xBF)
							return;	// disconnect
						else if (packet.Length >= 5 && packet[4] == 0x65)
							return;	// sync
						else if (frame == 0)
						{
							/* decode this to recover lobbyinfo, etc */
							var orders = packet.ToOrderList(null);
							foreach (var o in orders)
								if (o.OrderString == "StartGame")
									hasSeenGameStart = true;
								else if (o.OrderString == "SyncInfo" && !hasSeenGameStart)
									lobbyInfo = Session.Deserialize(o.TargetString);
						}
						else
							lastFrame = Math.Max(lastFrame, frame);
					});

			Duration = lastFrame * Game.NetTickScale;
			LobbyInfo = lobbyInfo;
		}

		public Map Map()
		{
			if (LobbyInfo == null)
				return null;

			var map = LobbyInfo.GlobalSettings.Map;
			if (!Game.modData.AvailableMaps.ContainsKey(map))
				return null;

			return Game.modData.AvailableMaps[map];
		}
	}
}
