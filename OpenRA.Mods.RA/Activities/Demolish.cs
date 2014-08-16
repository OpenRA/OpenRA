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
		readonly Target target;
		readonly int delay;
		readonly int flashes;
		readonly int flashesDelay;
		readonly int flashInterval;
		readonly int flashDuration;

		public Demolish(Actor target, int delay, int flashes, int flashesDelay, int flashInterval, int flashDuration)
		{
			this.target = Target.FromActor(target);
			this.delay = delay;
			this.flashes = flashes;
			this.flashesDelay = flashesDelay;
			this.flashInterval = flashInterval;
			this.flashDuration = flashDuration;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			self.World.AddFrameEndTask(w =>
			{
				for (var f = 0; f < flashes; f++)
					w.Add(new DelayedAction(flashesDelay + f * flashInterval, () =>
						w.Add(new FlashTarget(target.Actor, ticks: flashDuration))));

				w.Add(new DelayedAction(delay, () =>
				{
					// Can't demolish an already dead actor
					if (target.Type != TargetType.Actor)
						return;



					var demolishable = target.Actor.TraitOrDefault<IDemolishable>();
					if (demolishable == null || !demolishable.IsValidTarget(target.Actor, self))
						return;

					var modifiers = target.Actor.TraitsImplementing<IDamageModifier>()
						.Concat(self.Owner.PlayerActor.TraitsImplementing<IDamageModifier>())
						.Select(t => t.GetDamageModifier(self, null));

					if (Util.ApplyPercentageModifiers(100, modifiers) > 0)
						demolishable.Demolish(target.Actor, self);
				}));
			});

			return NextActivity;
		}
	}
}
