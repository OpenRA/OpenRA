#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Infiltrate : Enter
	{
		readonly Actor target;
		public Infiltrate(Actor self, Actor target)
			: base(self, target)
		{
			this.target = target;
		}

		protected override void OnInside(Actor self)
		{
			if (target.Flagged(ActorFlag.Dead) || target.Owner == self.Owner)
				return;

			foreach (var t in target.TraitsImplementing<INotifyInfiltrated>())
				t.Infiltrated(target, self);

			self.Destroy();

			if (target.HasTrait<Building>())
				Sound.PlayToPlayer(self.Owner, "bldginf1.aud");
		}
	}
}
