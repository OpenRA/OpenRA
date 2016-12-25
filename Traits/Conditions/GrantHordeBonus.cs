#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants horde types for the HordeBonus traits to work.")]
	public class GrantHordeBonusInfo : ITraitInfo
	{
		public readonly string HordeType = "horde";

		public object Create(ActorInitializer init) { return new GrantHordeBonus(init.Self, this); }
	}

	public class GrantHordeBonus
	{
		public readonly GrantHordeBonusInfo Info;

		public GrantHordeBonus(Actor self, GrantHordeBonusInfo info)
		{
			Info = info;
		}
	}
}
