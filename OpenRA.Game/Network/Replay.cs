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
	public class Replay
	{
		public readonly string Filename;
		public readonly int Duration;
		public readonly Session LobbyInfo;

		public Replay(string filename)
		{
			Filename = filename;

			using (var conn = new ReplayConnection(filename))
			{
				Duration = conn.TickCount * Game.NetTickScale;
				LobbyInfo = conn.LobbyInfo;
			}
		}

		public Map Map()
		{
			if (LobbyInfo == null)
				return null;

			var map = LobbyInfo.GlobalSettings.Map;
			return Game.modData.MapCache[map].Map;
		}
	}
}
