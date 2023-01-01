#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class Demolish : Enter
	{
		readonly int delay;
		readonly int flashes;
		readonly int flashesDelay;
		readonly int flashInterval;
		readonly BitSet<DamageType> damageTypes;
		readonly INotifyDemolition[] notifiers;
		readonly EnterBehaviour enterBehaviour;

		Actor enterActor;
		IDemolishable[] enterDemolishables;

		public Demolish(Actor self, in Target target, EnterBehaviour enterBehaviour, int delay, int flashes,
			int flashesDelay, int flashInterval, BitSet<DamageType> damageTypes, Color? targetLineColor)
			: base(self, target, targetLineColor)
		{
			notifiers = self.TraitsImplementing<INotifyDemolition>().ToArray();
			this.delay = delay;
			this.flashes = flashes;
			this.flashesDelay = flashesDelay;
			this.flashInterval = flashInterval;
			this.damageTypes = damageTypes;
			this.enterBehaviour = enterBehaviour;
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			enterActor = targetActor;
			enterDemolishables = targetActor.TraitsImplementing<IDemolishable>().ToArray();

			// Make sure we can still demolish the target before entering
			// (but not before, because this may stop the actor in the middle of nowhere)
			if (!enterDemolishables.Any(i => i.IsValidTarget(enterActor, self)))
			{
				Cancel(self, true);
				return false;
			}

			return true;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			self.World.AddFrameEndTask(w =>
			{
				// Make sure the target hasn't changed while entering
				// OnEnterComplete is only called if targetActor is alive
				if (targetActor != enterActor)
					return;

				if (!enterDemolishables.Any(i => i.IsValidTarget(enterActor, self)))
					return;

				w.Add(new FlashTarget(enterActor, Color.White, count: flashes, interval: flashInterval, delay: flashesDelay));

				foreach (var ind in notifiers)
					ind.Demolishing(self);

				foreach (var d in enterDemolishables)
					d.Demolish(enterActor, self, delay, damageTypes);

				if (enterBehaviour == EnterBehaviour.Dispose)
					self.Dispose();
				else if (enterBehaviour == EnterBehaviour.Suicide)
					self.Kill(self);
			});
		}
	}
}
