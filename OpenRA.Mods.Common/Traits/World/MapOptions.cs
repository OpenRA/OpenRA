#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	[TraitLocation(SystemActors.World)]
	[Desc("Controls the game speed, tech level, and short game lobby options.")]
	public class MapOptionsInfo : TraitInfo, ILobbyOptions, IRulesetLoaded
	{
		[Desc("Descriptive label for the short game checkbox in the lobby.")]
		public readonly string ShortGameCheckboxLabel = "Short Game";

		[Desc("Tooltip description for the short game checkbox in the lobby.")]
		public readonly string ShortGameCheckboxDescription = "Players are defeated when their bases are destroyed";

		[Desc("Default value of the short game checkbox in the lobby.")]
		public readonly bool ShortGameCheckboxEnabled = true;

		[Desc("Prevent the short game enabled state from being changed in the lobby.")]
		public readonly bool ShortGameCheckboxLocked = false;

		[Desc("Whether to display the short game checkbox in the lobby.")]
		public readonly bool ShortGameCheckboxVisible = true;

		[Desc("Display order for the short game checkbox in the lobby.")]
		public readonly int ShortGameCheckboxDisplayOrder = 0;

		[Desc("Descriptive label for the tech level option in the lobby.")]
		public readonly string TechLevelDropdownLabel = "Tech Level";

		[Desc("Tooltip description for the tech level option in the lobby.")]
		public readonly string TechLevelDropdownDescription = "The units and abilities that players can use";

		[Desc("Default tech level.")]
		public readonly string TechLevel = "unrestricted";

		[Desc("Prevent the tech level from being changed in the lobby.")]
		public readonly bool TechLevelDropdownLocked = false;

		[Desc("Display the tech level option in the lobby.")]
		public readonly bool TechLevelDropdownVisible = true;

		[Desc("Display order for the tech level option in the lobby.")]
		public readonly int TechLevelDropdownDisplayOrder = 0;

		[Desc("Tooltip description for the game speed option in the lobby.")]
		public readonly string GameSpeedDropdownLabel = "Game Speed";

		[Desc("Description of the game speed option in the lobby.")]
		public readonly string GameSpeedDropdownDescription = "The rate at which time passes";

		[Desc("Default game speed (leave empty to use the default defined in mod.yaml).")]
		public readonly string GameSpeed = null;

		[Desc("Prevent the game speed from being changed in the lobby.")]
		public readonly bool GameSpeedDropdownLocked = false;

		[Desc("Display the game speed option in the lobby.")]
		public readonly bool GameSpeedDropdownVisible = true;

		[Desc("Display order for the game speed option in the lobby.")]
		public readonly int GameSpeedDropdownDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption("shortgame", ShortGameCheckboxLabel, ShortGameCheckboxDescription,
				ShortGameCheckboxVisible, ShortGameCheckboxDisplayOrder, ShortGameCheckboxEnabled, ShortGameCheckboxLocked);

			var techLevels = map.PlayerActorInfo.TraitInfos<ProvidesTechPrerequisiteInfo>()
				.ToDictionary(t => t.Id, t => t.Name);

			if (techLevels.Count > 0)
				yield return new LobbyOption("techlevel", TechLevelDropdownLabel, TechLevelDropdownDescription,	TechLevelDropdownVisible, TechLevelDropdownDisplayOrder,
					techLevels, TechLevel, TechLevelDropdownLocked);

			var gameSpeeds = Game.ModData.Manifest.Get<GameSpeeds>();
			var speeds = gameSpeeds.Speeds.ToDictionary(s => s.Key, s => s.Value.Name);

			// NOTE: This is just exposing the UI, the backend logic for this option is hardcoded in World
			yield return new LobbyOption("gamespeed", GameSpeedDropdownLabel, GameSpeedDropdownDescription, GameSpeedDropdownVisible, GameSpeedDropdownDisplayOrder,
				speeds, GameSpeed ?? gameSpeeds.DefaultSpeed, GameSpeedDropdownLocked);
		}

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			var gameSpeeds = Game.ModData.Manifest.Get<GameSpeeds>().Speeds;
			if (GameSpeed != null && !gameSpeeds.ContainsKey(GameSpeed))
				throw new YamlException($"Invalid default game speed '{GameSpeed}'.");
		}

		public override object Create(ActorInitializer init) { return new MapOptions(this); }
	}

	public class MapOptions : INotifyCreated
	{
		readonly MapOptionsInfo info;

		public bool ShortGame { get; private set; }
		public string TechLevel { get; private set; }

		public MapOptions(MapOptionsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			ShortGame = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("shortgame", info.ShortGameCheckboxEnabled);

			TechLevel = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("techlevel", info.TechLevel);
		}
	}
}
