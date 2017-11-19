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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the game speed, tech level, and short game lobby options.")]
	public class MapOptionsInfo : ITraitInfo, ILobbyOptions, IRulesetLoaded
	{
		[Translate]
		[Desc("Descriptive label for the short game checkbox in the lobby.")]
		public readonly string ShortGameLabel = "Short Game";

		[Translate]
		[Desc("Tooltip description for the short game checkbox in the lobby.")]
		public readonly string ShortGameDescription = "Players are defeated when their bases are destroyed";

		[Desc("Default value of the short game checkbox in the lobby.")]
		public readonly bool ShortGameEnabled = true;

		[Desc("Prevent the short game enabled state from being changed in the lobby.")]
		public readonly bool ShortGameLocked = false;

		[Desc("Whether to display the short game checkbox in the lobby.")]
		public readonly bool ShortGameVisible = true;

		[Desc("Display order for the short game checkbox in the lobby.")]
		public readonly int ShortGameDisplayOrder = 0;

		[Translate]
		[Desc("Descriptive label for the tech level option in the lobby.")]
		public readonly string TechLevelLabel = "Tech Level";

		[Translate]
		[Desc("Tooltip description for the tech level option in the lobby.")]
		public readonly string TechLevelDescription = "Change the units and abilities at your disposal";

		[Desc("Default tech level.")]
		public readonly string TechLevel = "unrestricted";

		[Desc("Prevent the tech level from being changed in the lobby.")]
		public readonly bool TechLevelLocked = false;

		[Desc("Display the tech level option in the lobby.")]
		public readonly bool TechLevelVisible = true;

		[Desc("Display order for the tech level option in the lobby.")]
		public readonly int TechLevelDisplayOrder = 0;

		[Translate]
		[Desc("Tooltip description for the game speed option in the lobby.")]
		public readonly string GameSpeedLabel = "Game Speed";

		[Translate]
		[Desc("Description of the game speed option in the lobby.")]
		public readonly string GameSpeedDescription = "Change the rate at which time passes";

		[Desc("Default game speed.")]
		public readonly string GameSpeed = "default";

		[Desc("Prevent the game speed from being changed in the lobby.")]
		public readonly bool GameSpeedLocked = false;

		[Desc("Display the game speed option in the lobby.")]
		public readonly bool GameSpeedVisible = true;

		[Desc("Display order for the game speed option in the lobby.")]
		public readonly int GameSpeedDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("shortgame", ShortGameLabel, ShortGameDescription,
				ShortGameVisible, ShortGameDisplayOrder, ShortGameEnabled, ShortGameLocked);

			var techLevels = rules.Actors["player"].TraitInfos<ProvidesTechPrerequisiteInfo>()
				.ToDictionary(t => t.Id, t => t.Name);

			if (techLevels.Any())
				yield return new LobbyOption("techlevel", TechLevelLabel, TechLevelDescription,	TechLevelVisible, TechLevelDisplayOrder,
					new ReadOnlyDictionary<string, string>(techLevels),	TechLevel, TechLevelLocked);

			var gameSpeeds = Game.ModData.Manifest.Get<GameSpeeds>().Speeds
				.ToDictionary(s => s.Key, s => s.Value.Name);

			// NOTE: The server hardcodes special-case logic for this option id
			yield return new LobbyOption("gamespeed", GameSpeedLabel, GameSpeedDescription, GameSpeedVisible, GameSpeedDisplayOrder,
				new ReadOnlyDictionary<string, string>(gameSpeeds), GameSpeed, GameSpeedLocked);
		}

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			var gameSpeeds = Game.ModData.Manifest.Get<GameSpeeds>().Speeds;
			if (!gameSpeeds.ContainsKey(GameSpeed))
				throw new YamlException("Invalid default game speed '{0}'.".F(GameSpeed));
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
