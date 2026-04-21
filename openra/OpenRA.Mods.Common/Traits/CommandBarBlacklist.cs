#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Blacklist certain order types to disable on the command bar when this unit is selected.")]
	public class CommandBarBlacklistInfo : TraitInfo<CommandBarBlacklist>
	{
		[Desc("Disable the 'Stop' button for this actor.")]
		public readonly bool DisableStop = true;

		[Desc("Disable the 'Waypoint Mode' button for this actor.")]
		public readonly bool DisableWaypointMode = true;
	}

	public class CommandBarBlacklist { }
}
