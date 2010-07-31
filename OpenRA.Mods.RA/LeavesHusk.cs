#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class LeavesHuskInfo : TraitInfo<LeavesHusk>
	{
		[ActorReference]
		public readonly string HuskActor = null;
	}

	class LeavesHusk : INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				self.World.AddFrameEndTask(w =>
					{
						var info = self.Info.Traits.Get<LeavesHuskInfo>();
						var husk = w.CreateActor(info.HuskActor, self.Location, self.Owner);
						husk.CenterLocation = self.CenterLocation;
						husk.traits.Get<IFacing>().Facing = self.traits.Get<IFacing>().Facing;

						var turreted = self.traits.GetOrDefault<Turreted>();
						if (turreted != null)
							foreach (var p in husk.traits.WithInterface<ThrowsParticle>())
								p.InitialFacing = turreted.turretFacing;
					});
		}
	}
}
