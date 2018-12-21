#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Collects all GameMode traits and enables choosing between them in the lobby.",
		"Use this to enable different game modes on the same map. Attach this to the Player actor.")]
	class GameModeManagerInfo : ITraitInfo, ILobbyOptions, Requires<ConditionManagerInfo>, Requires<GameModeInfo>
	{
		public enum DropdownVisibility { Auto, Shown, Hidden }

		[Translate]
		[Desc("Descriptive label for the game mode option in the lobby.")]
		public readonly string GameModeLabel = "Game Mode";

		[Translate]
		[Desc("Tooltip description for the game mode option in the lobby.")]
		public readonly string GameModeDescription = "Select the game mode";

		[Desc("Prevent the game mode option from being changed in the lobby.")]
		public readonly bool GameModeLocked = false;

		[Desc("Whether to display the game mode option in the lobby.",
			"Setting this to 'Auto' will hide the dropdown if only one option is available.")]
		public readonly DropdownVisibility GameModeVisible = DropdownVisibility.Auto;

		[Desc("Display order for the game mode option in the lobby.")]
		public readonly int GameModeDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var modes = rules.Actors["player"].TraitInfos<GameModeInfo>().Concat(rules.Actors["world"].TraitInfos<GameModeInfo>())
				.Select(m => new KeyValuePair<string, string>(m.InternalName, m.Name)).ToDictionary(x => x.Key, x => x.Value);
			var dropdownVisible = GameModeVisible == DropdownVisibility.Shown || (GameModeVisible == DropdownVisibility.Auto && modes.Count > 1);
			var defaultValue = modes.Count > 0 ? modes.First().Key : "none";

			yield return new LobbyOption("gamemode", GameModeLabel, GameModeDescription, dropdownVisible, GameModeDisplayOrder,
				new ReadOnlyDictionary<string, string>(modes), defaultValue, GameModeLocked);
		}

		public object Create(ActorInitializer init) { return new GameModeManager(init.Self, this); }
	}

	class GameModeManager : IPreventMapSpawn
	{
		public readonly GameMode ActiveGameMode;

		public GameModeManager(Actor self, GameModeManagerInfo info)
		{
			var cm = self.Trait<ConditionManager>();
			var wcm = self.World.WorldActor.Trait<ConditionManager>();

			var mode = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("gamemode", "shellmap");
			ActiveGameMode = self.TraitsImplementing<GameMode>().Concat(self.World.WorldActor.TraitsImplementing<GameMode>())
				.Where(m => m.Info.InternalName == mode).First();

			if (ActiveGameMode.Info.Condition != null)
			{
				cm.GrantCondition(self, ActiveGameMode.Info.Condition);
				wcm.GrantCondition(self.World.WorldActor, ActiveGameMode.Info.Condition);
			}
		}

		bool IPreventMapSpawn.PreventMapSpawn(World world, ActorReference actorReference)
		{
			string[] gameModes = { };
			if (actorReference.InitDict.Contains<GameModesInit>())
				gameModes = actorReference.InitDict.Get<GameModesInit>().Value(world);

			if (gameModes.Any() && !gameModes.Contains(world.LobbyInfo.GlobalSettings.OptionOrDefault("gamemode", "")))
				return true;

			return false;
		}
	}

	// GameModeInit is used to restrict Actors to certain game modes, eg. only showing KotH flags on the koth game mode.
	// An empty value is interpreted to mean that the actor should appear in all game modes.
	// Matches against the InternalName field of a GameMode.
	public class GameModesInit : IActorInit<string[]>
	{
		[FieldFromYamlKey]
		readonly string[] value = { };
		public GameModesInit() { }
		public GameModesInit(string[] init) { value = init; }
		public string[] Value(World world) { return value; }
	}
}
