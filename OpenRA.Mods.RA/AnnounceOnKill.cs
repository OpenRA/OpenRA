#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Play the Kill voice of this actor when eliminating enemies.")]
	public class AnnounceOnKillInfo : TraitInfo<AnnounceOnKill> { }

	public class AnnounceOnKill : INotifyAppliedDamage
	{
		public void AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				Sound.PlayVoice("Kill", self, self.Owner.Country.Race);
		}
	}
}
