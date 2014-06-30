#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Mission")]
	public class MissionProperties : ScriptPlayerProperties
	{
		public MissionProperties(Player player)
			: base(player) { }

		[Desc("True when the player is victorious.")]
		public bool Won
		{
			get { return player.WinState == WinState.Won; }
			set { player.WinState = WinState.Won; }
		}

		[Desc("True when the player has been defeated.")]
		public bool Lost
		{
			get { return player.WinState == WinState.Lost; }
			set { player.WinState = WinState.Lost; }
		}
	}
}