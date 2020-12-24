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
	[Desc("Controls the build radius checkboxes in the lobby options.")]
	public class MapBuildRadiusInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Descriptive label for the ally build radius checkbox in the lobby.")]
		public readonly string AllyBuildRadiusCheckboxLabel = "Build off Allies";

		[Desc("Tooltip description for the ally build radius checkbox in the lobby.")]
		public readonly string AllyBuildRadiusCheckboxDescription = "Allow allies to place structures inside your build area";

		[Desc("Default value of the ally build radius checkbox in the lobby.")]
		public readonly bool AllyBuildRadiusCheckboxEnabled = true;

		[Desc("Prevent the ally build radius state from being changed in the lobby.")]
		public readonly bool AllyBuildRadiusCheckboxLocked = false;

		[Desc("Whether to display the ally build radius checkbox in the lobby.")]
		public readonly bool AllyBuildRadiusCheckboxVisible = true;

		[Desc("Display order for the ally build radius checkbox in the lobby.")]
		public readonly int AllyBuildRadiusCheckboxDisplayOrder = 0;

		[Desc("Tooltip description for the build radius checkbox in the lobby.")]
		public readonly string BuildRadiusCheckboxLabel = "Limit Build Area";

		[Desc("Tooltip description for the build radius checkbox in the lobby.")]
		public readonly string BuildRadiusCheckboxDescription = "Limits structure placement to areas around Construction Yards";

		[Desc("Default value of the build radius checkbox in the lobby.")]
		public readonly bool BuildRadiusCheckboxEnabled = true;

		[Desc("Prevent the build radius state from being changed in the lobby.")]
		public readonly bool BuildRadiusCheckboxLocked = false;

		[Desc("Display the build radius checkbox in the lobby.")]
		public readonly bool BuildRadiusCheckboxVisible = true;

		[Desc("Display order for the build radius checkbox in the lobby.")]
		public readonly int BuildRadiusCheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("allybuild", AllyBuildRadiusCheckboxLabel, AllyBuildRadiusCheckboxDescription,
				AllyBuildRadiusCheckboxVisible, AllyBuildRadiusCheckboxDisplayOrder, AllyBuildRadiusCheckboxEnabled, AllyBuildRadiusCheckboxLocked);

			yield return new LobbyBooleanOption("buildradius", BuildRadiusCheckboxLabel, BuildRadiusCheckboxDescription,
				BuildRadiusCheckboxVisible, BuildRadiusCheckboxDisplayOrder, BuildRadiusCheckboxEnabled, BuildRadiusCheckboxLocked);
		}

		public override object Create(ActorInitializer init) { return new MapBuildRadius(this); }
	}

	public class MapBuildRadius : INotifyCreated
	{
		readonly MapBuildRadiusInfo info;
		public bool AllyBuildRadiusEnabled { get; private set; }
		public bool BuildRadiusEnabled { get; private set; }

		public MapBuildRadius(MapBuildRadiusInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			AllyBuildRadiusEnabled = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("allybuild", info.AllyBuildRadiusCheckboxEnabled);
			BuildRadiusEnabled = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("buildradius", info.BuildRadiusCheckboxEnabled);
		}
	}
}
