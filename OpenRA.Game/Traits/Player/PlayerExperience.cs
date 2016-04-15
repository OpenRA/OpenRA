#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;

namespace OpenRA.Traits
{
	[Desc("Attach this to the player actor.")]
	public class PlayerExperienceInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new PlayerExperience(init.Self, this); }
	}

	public class PlayerExperience : ISync
	{
		[Sync] public int Experience { get; private set; }

		public PlayerExperience(Actor self, PlayerExperienceInfo info)
		{
			Experience = 0;
		}

		public void GivePlayerExperience(int num)
		{
			Experience += num;
		}
	}
}
