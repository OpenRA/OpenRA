#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class EnterCarrierParent : Enter
	{
		readonly Actor parent;
		readonly CarrierParent spawnerParent;

		public EnterCarrierParent(Actor self, Actor parent, CarrierParent spawnerParent, EnterBehaviour enterBehaviour)
			: base(self, Target.FromActor(parent))
		{
			this.parent = parent;
			this.spawnerParent = spawnerParent;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			if (parent.IsDead)
				return;

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || parent.IsDead)
					return;

				spawnerParent.PickupChild(parent, self);
				w.Remove(self);

				// Delayed launching is handled at spawner.
				var ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
				if (ammoPools != null)
					foreach (var pool in ammoPools)
						pool.GiveAmmo(self, pool.Info.Ammo);
			});
		}
	}
}
