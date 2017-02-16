#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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

		public readonly bool IsCompatible = false;
		public readonly bool IsJoinable = false;

		public readonly string ModLabel = "";
		public readonly string ModId = "";
		public readonly string ModVersion = "";

		public GameServer(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);

			Manifest mod;
			ExternalMod external;
			var modVersion = Mods.Split('@');

			ModLabel = "Unknown mod: {0}".F(Mods);
			if (modVersion.Length == 2)
			{
				ModId = modVersion[0];
				ModVersion = modVersion[1];

				var externalKey = ExternalMod.MakeKey(modVersion[0], modVersion[1]);
				if (Game.ExternalMods.TryGetValue(externalKey, out external)
					&& external.Version == modVersion[1])
				{
					ModLabel = "{0} ({1})".F(external.Title, external.Version);
					IsCompatible = true;
				}
				else if (Game.Mods.TryGetValue(modVersion[0], out mod))
				{
					// Use internal mod data to populate the section header, but
					// on-connect switching must use the external mod plumbing.
					ModLabel = "{0} ({1})".F(mod.Metadata.Title, modVersion[1]);
				}
			}

			var mapAvailable = Game.Settings.Game.AllowDownloading || Game.ModData.MapCache[Map].Status == MapStatus.Available;
			IsJoinable = IsCompatible && State == 1 && mapAvailable;
		}
	}
}
