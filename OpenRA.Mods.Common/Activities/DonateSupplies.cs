#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class DonateSupplies : Enter
	{
		readonly Actor target;
		readonly int payload;
		readonly int experience;

		public DonateSupplies(Actor self, Actor target, int payload, int playerExperience)
			: base(self, target, EnterBehaviour.Dispose)
		{
			this.target = target;
			this.payload = payload;
			this.experience = playerExperience;
		}

		protected override void OnInside(Actor self)
		{
			if (target.IsDead)
				return;

			target.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(payload);

			var exp = self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
			if (exp != null && target.Owner != self.Owner)
				exp.GiveExperience(experience);

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				self.World.AddFrameEndTask(w => w.Add(new FloatingText(target.CenterPosition, target.Owner.Color.RGB, FloatingText.FormatCashTick(payload), 30)));
		}
	}
}
