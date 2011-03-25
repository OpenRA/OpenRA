#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	public class AnnounceOnBuildInfo : ITraitInfo
	{		
		public object Create(ActorInitializer init) { return new AnnounceOnBuild(init.self); }
	}

	public class AnnounceOnBuild
	{	
		public AnnounceOnBuild(Actor self)
		{
			Sound.PlayVoice("Build", self, self.Owner.Country.Race);
		}
	}
	
	public class AnnounceOnKillInfo : TraitInfo<AnnounceOnKill> {}

	public class AnnounceOnKill : INotifyAppliedDamage
	{	
		public void AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				Sound.PlayVoice("Kill", self, self.Owner.Country.Race);
		}
	}
}
