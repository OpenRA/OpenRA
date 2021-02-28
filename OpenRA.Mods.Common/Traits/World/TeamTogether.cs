#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the team together checkbox in the lobby options.")]
	public class TeamTogetherInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Descriptive label for the teamed faction checkbox in the lobby.")]
		public readonly string TeamedFactionCheckboxLabel = "Teamed Faction";

		[Desc("Tooltip description for the teamed faction checkbox in the lobby.")]
		public readonly string TeamedFactionCheckboxDescription = "All players in a team control one faction.";

		[Desc("Default value of the command allied units checkbox in the lobby.")]
		public readonly bool TeamedFactionCheckboxEnabled = false;

		[Desc("Prevent the command allied units state from being changed in the lobby.")]
		public readonly bool TeamedFactionCheckboxLocked = false;

		[Desc("Whether to display the command allied units checkbox in the lobby.")]
		public readonly bool TeamedFactionCheckboxVisible = true;

		[Desc("Display order for the command allied units checkbox in the lobby.")]
		public readonly int TeamedFactionCheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("teamedfaction", TeamedFactionCheckboxLabel, TeamedFactionCheckboxDescription,
				TeamedFactionCheckboxVisible, TeamedFactionCheckboxDisplayOrder, TeamedFactionCheckboxEnabled, TeamedFactionCheckboxLocked);
		}

		public override object Create(ActorInitializer init) { return new TeamTogether(this); }
	}

	public class TeamTogether : INotifyCreated
	{
		readonly TeamTogetherInfo info;
		public bool TeamedFactionEnabled { get; private set; }

		public TeamTogether(TeamTogetherInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			TeamedFactionEnabled = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("teamedfaction", info.TeamedFactionCheckboxEnabled);
		}
	}
}
