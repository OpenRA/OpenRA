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
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Demolish : Activity
	{
		Target target;
		int delay;

		public Demolish(Actor target, int delay)
		{
			this.target = Target.FromActor(target);
			this.delay = delay;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			self.World.Add(new DelayedAction(delay * 1/3, () =>
				self.World.Add(new FlashTarget(target.Actor, 2))));

			self.World.Add(new DelayedAction(delay * 2/3, () =>
				self.World.Add(new FlashTarget(target.Actor, 2))));

			self.World.Add(new FlashTarget(target.Actor, 2));

			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(delay, () =>
			{
				// Can't demolish an already dead actor
				if (target.Type != TargetType.Actor)
					return;

				// Invulnerable actors can't be demolished
				var modifier = (float)target.Actor.TraitsImplementing<IDamageModifier>()
					.Concat(self.Owner.PlayerActor.TraitsImplementing<IDamageModifier>())
					.Select(t => t.GetDamageModifier(self, null)).Product();

				var demolishable = target.Actor.TraitOrDefault<IDemolishable>();
					if (demolishable == null || !demolishable.IsValidTarget(target.Actor, self))
					return;

				if (modifier > 0)
					demolishable.Demolish(target.Actor, self);
			})));

			return NextActivity;
		}
	}
}
