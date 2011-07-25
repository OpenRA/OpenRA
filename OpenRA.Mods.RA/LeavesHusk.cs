#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA
{
	class LeavesHuskInfo : TraitInfo<LeavesHusk>
	{
		[ActorReference]
		public readonly string HuskActor = null;
	}

	class LeavesHusk : INotifyKilled
	{
		public void Killed(Actor self, AttackInfo e)
		{
			self.World.AddFrameEndTask(w =>
			{
				var info = self.Info.Traits.Get<LeavesHuskInfo>();
				var td = new TypeDictionary()
				{
					new LocationInit( self.Location ),
					new CenterLocationInit(self.CenterLocation),
					new OwnerInit( self.Owner ),
					new SkipMakeAnimsInit()
				};

				if (self.HasTrait<IFacing>())
					td.Add(new FacingInit( self.Trait<IFacing>().Facing ));

				// Allows the husk to drag to its final position
	            var mobile = self.TraitOrDefault<Mobile>();
	            if (mobile != null)
					td.Add(new HuskSpeedInit(mobile.MovementSpeedForCell(self, self.Location)));

				var husk = w.CreateActor(info.HuskActor, td);
				var turreted = self.TraitOrDefault<Turreted>();
				if (turreted != null)
					foreach (var p in husk.TraitsImplementing<ThrowsParticle>())
						p.InitialFacing = turreted.turretFacing;
			});
		}
	}
}
