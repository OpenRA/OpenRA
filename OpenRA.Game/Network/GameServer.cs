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

			ModMetadata mod;
			var modVersion = Mods.Split('@');
			if (modVersion.Length == 2 && ModMetadata.AllMods.TryGetValue(modVersion[0], out mod))
			{
				ModId = modVersion[0];
				ModVersion = modVersion[1];
				ModLabel = "{0} ({1})".F(mod.Title, modVersion[1]);
				IsCompatible = Game.Settings.Debug.IgnoreVersionMismatch || ModVersion == mod.Version;
			}
			else
				ModLabel = "Unknown mod: {0}".F(Mods);

			var mapAvailable = Game.Settings.Game.AllowDownloading || Game.ModData.MapCache[Map].Status == MapStatus.Available;
			IsJoinable = IsCompatible && State == 1 && mapAvailable;
		}
	}
}
