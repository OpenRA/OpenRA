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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the build radius checkboxes in the lobby options.")]
	public class MapBuildRadiusInfo : TraitInfo<MapBuildRadius>
	{
		[Desc("Default value of the ally build radius checkbox in the lobby.")]
		public readonly bool AllyBuildRadiusEnabled = true;

		[Desc("Prevent the ally build radius state from being changed in the lobby.")]
		public readonly bool AllyBuildRadiusLocked = false;
	}

	public class MapBuildRadius : INotifyCreated
	{
		public bool AllyBuildRadiusEnabled { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			AllyBuildRadiusEnabled = self.World.LobbyInfo.GlobalSettings.AllyBuildRadius;
		}
	}
}
