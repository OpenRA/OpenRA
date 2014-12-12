#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Network
{
	public class GameServer
	{
		public readonly int Id = 0;
		public readonly string Name = null;
		public readonly string Address = null;
		public readonly int State = 0;
		public readonly int Players = 0;
		public readonly int MaxPlayers = 0;
		public readonly int Bots = 0;
		public readonly int Spectators = 0;
		public readonly string Map = null;
		public readonly string Mods = "";
		public readonly int TTL = 0;
		public readonly bool Protected = false;
		public readonly string Started = null;

		public bool CanJoin()
		{
			// "waiting for players"
			if (State != 1)
				return false;

			if (!CompatibleVersion())
				return false;

			// Don't have the map locally
			// TODO: We allow joining, then drop on game start if the map isn't available
			if (Game.modData.MapCache[Map].Status != MapStatus.Available && !Game.Settings.Game.AllowDownloading)
				return false;

			return true;
		}

		public bool CompatibleVersion()
		{
			// Invalid game listing - we require one entry of id@version
			var modVersion = Mods.Split('@');
			if (modVersion.Length != 2)
				return false;

			// Mod is not installed
			if (!ModMetadata.AllMods.ContainsKey(modVersion[0]))
				return false;

			// Compatible mod, but different version
			var mod = Game.modData.Manifest.Mod;
			if (modVersion[1] != mod.Version && !Game.Settings.Debug.IgnoreVersionMismatch)
				return false;

			return true;
		}
	}
}
