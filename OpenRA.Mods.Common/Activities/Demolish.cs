#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
		readonly INotifyDemolition[] notifiers;

		public Demolish(Actor self, Actor target, EnterBehaviour enterBehaviour, int delay,
			int flashes, int flashesDelay, int flashInterval)
			: base(self, target, enterBehaviour)
		{
			this.target = target;
			demolishables = target.TraitsImplementing<IDemolishable>().ToArray();
			notifiers = self.TraitsImplementing<INotifyDemolition>().ToArray();
			this.delay = delay;
			this.flashes = flashes;
			this.flashesDelay = flashesDelay;
			this.flashInterval = flashInterval;
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

				w.Add(new FlashTarget(target, count: flashes, delay: flashesDelay, interval: flashInterval));

				foreach (var ind in notifiers)
					ind.Demolishing(self);

				foreach (var d in demolishables)
					d.Demolish(target, self, delay);
			});
		}
	}
}
