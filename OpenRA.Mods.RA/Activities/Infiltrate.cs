#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Infiltrate : Activity
	{
		Target target;
		public Infiltrate(Actor target) { this.target = Target.FromActor(target); }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValid || target.Actor.Owner == self.Owner)
				return NextActivity;

			foreach (var t in target.Actor.TraitsImplementing<IAcceptInfiltrator>())
				t.OnInfiltrate(target.Actor, self);

			self.World.AddFrameEndTask(w => { if (!self.Destroyed) w.Remove(self); });

			if (target.Actor.HasTrait<Building>())
				Sound.PlayToPlayer(self.Owner, "bldginf1.aud");

			return this;
		}
	}
}
