#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class Demolish : Enter
	{
		readonly Actor target;
		readonly IDemolishable[] demolishables;
		readonly int delay;
		readonly int flashes;
		readonly int flashesDelay;
		readonly int flashInterval;
		readonly int flashDuration;
		readonly INotifyDemolition[] notifiers;

		public Demolish(Actor self, Actor target, EnterBehaviour enterBehaviour, int delay,
			int flashes, int flashesDelay, int flashInterval, int flashDuration)
			: base(self, target, enterBehaviour)
		{
			this.target = target;
			demolishables = target.TraitsImplementing<IDemolishable>().ToArray();
			notifiers = self.TraitsImplementing<INotifyDemolition>().ToArray();
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
				if (target.IsDead)
					return;

				foreach (var ind in notifiers)
					ind.Demolishing(self);

				for (var f = 0; f < flashes; f++)
					w.Add(new DelayedAction(flashesDelay + f * flashInterval, () =>
						w.Add(new FlashTarget(target, ticks: flashDuration))));

				w.Add(new DelayedAction(delay, () =>
				{
					if (target.IsDead)
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
