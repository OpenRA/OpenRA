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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the team together checkbox in the lobby options.")]
	public class TeamTogetherInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Descriptive label for the command allied units checkbox in the lobby.")]
		public readonly string CommandAlliedUnitsCheckboxLabel = "Command Allied Units";

		[Desc("Tooltip description for the command allied units checkbox in the lobby.")]
		public readonly string CommandAlliedUnitsCheckboxDescription = "Allow team members to command your units and like wise command team members units.";

		[Desc("Default value of the command allied units checkbox in the lobby.")]
		public readonly bool CommandAlliedUnitsCheckboxEnabled = false;

		[Desc("Prevent the command allied units state from being changed in the lobby.")]
		public readonly bool CommandAlliedUnitsCheckboxLocked = false;

		[Desc("Whether to display the command allied units checkbox in the lobby.")]
		public readonly bool CommandAlliedUnitsCheckboxVisible = true;

		[Desc("Display order for the command allied units checkbox in the lobby.")]
		public readonly int CommandAlliedUnitsCheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption("commandalliedunits", CommandAlliedUnitsCheckboxLabel, CommandAlliedUnitsCheckboxDescription,
				CommandAlliedUnitsCheckboxVisible, CommandAlliedUnitsCheckboxDisplayOrder, CommandAlliedUnitsCheckboxEnabled, CommandAlliedUnitsCheckboxLocked);
		}

		public override object Create(ActorInitializer init) { return TeamTogether.Instance; }
	}

	public class TeamTogether
	{
		public static readonly TeamTogether Instance = new TeamTogether();
		TeamTogether() { }
	}
}
