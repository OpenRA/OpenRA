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
	[Desc("Defines the FMVs that can be played by missions.")]
	[TraitLocation(SystemActors.World)]
	public class MissionDataInfo : TraitInfo<MissionData>
	{
		[FluentReference(optional: true)]
		[Desc("Briefing text displayed in the mission browser.")]
		public readonly string Briefing;

		[Desc("Played by the \"Background Info\" button in the mission browser.")]
		public readonly string BackgroundVideo;

		[Desc("Played by the \"Briefing\" button in the mission browser.")]
		public readonly string BriefingVideo;

		[Desc("Automatically played before starting the mission.")]
		public readonly string StartVideo;

		[Desc("Automatically played when the player wins the mission.")]
		public readonly string WinVideo;

		[Desc("Automatically played when the player loses the mission.")]
		public readonly string LossVideo;

		[Desc("Optional results widget to open after a singleplayer mission ends instead of the ingame menu.",
			"The common MISSION_RESULTS widget reuses the existing statistics panel.")]
		public readonly string GameOverScreen;

		[Desc("Optional Lua global called when continuing from the results screen, with a boolean indicating victory.",
			"The ingame menu opens if the function is absent or fails. Only used with GameOverScreen.")]
		public readonly string GameOverCallback;
	}

	public class MissionData { }
}
