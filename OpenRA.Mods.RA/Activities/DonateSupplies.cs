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
	class DonateSupplies : Enter
	{
		readonly Actor target;
		readonly int payload;

		public DonateSupplies(Actor self, Actor target, int payload)
			: base(self, target)
		{
			this.target = target;
			this.payload = payload;
		}

		protected override void OnInside(Actor self)
		{
			if (target.IsDead())
				return;

			target.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(payload);
			self.Destroy();

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				self.World.AddFrameEndTask(w => w.Add(new FloatingText(target.CenterPosition, target.Owner.Color.RGB, FloatingText.FormatCashTick(payload), 30)));
		}
	}
}
