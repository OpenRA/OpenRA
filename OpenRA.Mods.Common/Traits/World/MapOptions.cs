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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the map difficulty, tech level, and short game lobby options.")]
	public class MapOptionsInfo : ITraitInfo, ILobbyOptions
	{
		[Desc("Default value of the short game checkbox in the lobby.")]
		public readonly bool ShortGameEnabled = true;

		[Desc("Prevent the short game enabled state from being changed in the lobby.")]
		public readonly bool ShortGameLocked = false;

		[Desc("Default tech level.")]
		public readonly string TechLevel = "unrestricted";

		[Desc("Prevent the tech level from being changed in the lobby.")]
		public readonly bool TechLevelLocked = false;

		[Desc("Default game speed.")]
		public readonly string GameSpeed = "default";

		[Desc("Prevent the game speed from being changed in the lobby.")]
		public readonly bool GameSpeedLocked = false;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("shortgame", "Short Game", ShortGameEnabled, ShortGameLocked);

			var techLevels = rules.Actors["player"].TraitInfos<ProvidesTechPrerequisiteInfo>()
				.ToDictionary(t => t.Id, t => t.Name);

			if (techLevels.Any())
				yield return new LobbyOption("techlevel", "Tech Level",
					new ReadOnlyDictionary<string, string>(techLevels),
					TechLevel, TechLevelLocked);

			var gameSpeeds = Game.ModData.Manifest.Get<GameSpeeds>().Speeds
				.ToDictionary(s => s.Key, s => s.Value.Name);

			// NOTE: The server hardcodes special-case logic for this option id
			yield return new LobbyOption("gamespeed", "Game Speed",
				new ReadOnlyDictionary<string, string>(gameSpeeds),
				GameSpeed, GameSpeedLocked);
		}

		public object Create(ActorInitializer init) { return new MapOptions(this); }
	}

	public class MapOptions : INotifyCreated
	{
		readonly MapOptionsInfo info;

		public bool ShortGame { get; private set; }
		public string TechLevel { get; private set; }
		public GameSpeed GameSpeed { get; private set; }

		public MapOptions(MapOptionsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			ShortGame = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("shortgame", info.ShortGameEnabled);

			TechLevel = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("techlevel", info.TechLevel);

			var speed = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("gamespeed", info.GameSpeed);

			GameSpeed = Game.ModData.Manifest.Get<GameSpeeds>().Speeds[speed];
		}
	}
}
