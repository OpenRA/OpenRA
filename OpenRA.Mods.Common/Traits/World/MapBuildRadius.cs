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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the build radius checkboxes in the lobby options.")]
	public class MapBuildRadiusInfo : ITraitInfo, ILobbyOptions
	{
		[Desc("Default value of the ally build radius checkbox in the lobby.")]
		public readonly bool AllyBuildRadiusEnabled = true;

		[Desc("Prevent the ally build radius state from being changed in the lobby.")]
		public readonly bool AllyBuildRadiusLocked = false;

		[Desc("Default value of the build radius checkbox in the lobby.")]
		public readonly bool BuildRadiusEnabled = true;

		[Desc("Prevent the build radius state from being changed in the lobby.")]
		public readonly bool BuildRadiusLocked = false;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("allybuild", "Build off Allies' ConYards", AllyBuildRadiusEnabled, AllyBuildRadiusLocked);

			yield return new LobbyBooleanOption("buildradius", "Limit ConYard Area", BuildRadiusEnabled, BuildRadiusLocked);
		}

		public object Create(ActorInitializer init) { return new MapBuildRadius(this); }
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
				.OptionOrDefault("allybuild", info.AllyBuildRadiusEnabled);
			BuildRadiusEnabled = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("buildradius", info.BuildRadiusEnabled);
		}
	}
}
