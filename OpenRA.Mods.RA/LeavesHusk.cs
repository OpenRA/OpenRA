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
using OpenRA.FileFormats;

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
						var td = new TypeDictionary 
						{
							new LocationInit( self.Location ),
							new OwnerInit( self.Owner ),
							new SkipMakeAnimsInit()
						};
						
						if (self.HasTrait<IFacing>())
							td.Add(new FacingInit( self.Trait<IFacing>().Facing ));

						var husk = w.CreateActor(info.HuskActor, td);
						var turreted = self.TraitOrDefault<Turreted>();
						if (turreted != null)
							foreach (var p in husk.TraitsImplementing<ThrowsParticle>())
								p.InitialFacing = turreted.turretFacing;
					});
		}
	}
}
