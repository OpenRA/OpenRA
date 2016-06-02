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
	[Desc("Controls the map difficulty, tech level, and short game lobby options.")]
	public class MapOptionsInfo : TraitInfo<MapOptions>
	{
		[Desc("Default value of the short game checkbox in the lobby.")]
		public readonly bool ShortGameEnabled = true;

		[Desc("Prevent the short game enabled state from being changed in the lobby.")]
		public readonly bool ShortGameLocked = false;

		[Desc("Default tech level.")]
		public readonly string TechLevel = "Unrestricted";

		[Desc("Prevent the tech level from being changed in the lobby.")]
		public readonly bool TechLevelLocked = false;

		[Desc("Difficulty levels supported by the map.")]
		public readonly string[] Difficulties = { };

		[Desc("Default difficulty level.")]
		public readonly string Difficulty = null;

		[Desc("Prevent the difficulty from being changed in the lobby.")]
		public readonly bool DifficultyLocked = false;
	}

	public class MapOptions : INotifyCreated
	{
		public bool ShortGame { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			ShortGame = self.World.LobbyInfo.GlobalSettings.ShortGame;
		}
	}
}
