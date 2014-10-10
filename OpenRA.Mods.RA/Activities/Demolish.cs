#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Demolish : Enter
	{
		readonly Actor target;
		readonly IEnumerable<IDemolishable> demolishables;
		readonly int delay;
		readonly int flashes;
		readonly int flashesDelay;
		readonly int flashInterval;
		readonly int flashDuration;

		public Demolish(Actor self, Actor target, int delay, int flashes, int flashesDelay, int flashInterval, int flashDuration)
			: base(self, target)
		{
			this.target = target;
			demolishables = target.TraitsImplementing<IDemolishable>();
			this.delay = delay;
			this.flashes = flashes;
			this.flashesDelay = flashesDelay;
			this.flashInterval = flashInterval;
			this.flashDuration = flashDuration;
		}

		protected override bool CanReserve(Actor self)
		{
			return demolishables.Any(i => i.IsValidTarget(target, self));
		}

		protected override void OnInside(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (target.IsDead())
					return;

				for (var f = 0; f < flashes; f++)
					w.Add(new DelayedAction(flashesDelay + f * flashInterval, () =>
						w.Add(new FlashTarget(target, ticks: flashDuration))));

				w.Add(new DelayedAction(delay, () =>
				{
					if (target.IsDead())
						return;

					var modifiers = target.TraitsImplementing<IDamageModifier>()
						.Concat(self.Owner.PlayerActor.TraitsImplementing<IDamageModifier>())
						.Select(t => t.GetDamageModifier(self, null));

					if (Util.ApplyPercentageModifiers(100, modifiers) > 0)
						demolishables.Do(d => d.Demolish(target, self));
				}));
			});
		}
	}
}
