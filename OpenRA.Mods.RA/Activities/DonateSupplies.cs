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
using OpenRA.Traits;
using OpenRA.Mods.RA.Effects;

namespace OpenRA.Mods.RA.Activities
{
	class DonateSupplies : Activity
	{
		Actor target;
		int payload;

		public DonateSupplies(Actor target, int payload)
		{
			this.target = target;
			this.payload = payload;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (target == null || !target.IsInWorld || target.IsDead()) return NextActivity;
			if (!target.OccupiesSpace.OccupiedCells().Any(x => x.First == self.Location))
				return NextActivity;

			target.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(payload);
			self.Destroy();
			// TODO: does not care about per-player-shrouds in spectator mode
			if (self.World.ObserverMode || self.Owner.Stances[self.World.LocalPlayer] == Stance.Ally)
				self.World.AddFrameEndTask(w => w.Add(new CashTick(payload, 30, 2, target.CenterLocation, target.Owner.ColorRamp.GetColor(0))));

			return this;
		}
	}
}
