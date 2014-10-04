#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class DonateSupplies : Activity
	{
		Target target;
		int payload;

		public DonateSupplies(Actor target, int payload)
		{
			this.target = Target.FromActor(target);
			this.payload = payload;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			if (target.Type != TargetType.Actor)
				return NextActivity;

			var targetActor = target.Actor;
			targetActor.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(payload);
			self.Destroy();

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				self.World.AddFrameEndTask(w => w.Add(new FloatingText(targetActor.CenterPosition, targetActor.Owner.Color.RGB, FloatingText.FormatCashTick(payload), 30)));

			return this;
		}
	}
}
