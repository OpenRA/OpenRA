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

using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Display the time remaining until the super weapon attached to the actor is ready to the player and his allies.")]
	class SupportPowerChargeBarInfo : ITraitInfo
	{
		public readonly Color Color = Color.Magenta;

		public object Create(ActorInitializer init) { return new SupportPowerChargeBar(init.Self, this); }
	}

	class SupportPowerChargeBar : ISelectionBar
	{
		readonly Actor self;
		readonly SupportPowerChargeBarInfo info;

		public SupportPowerChargeBar(Actor self, SupportPowerChargeBarInfo info)
		{
			this.self = self;
			this.info = info;
		}

		float ISelectionBar.GetValue()
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			var spm = self.Owner.PlayerActor.Trait<SupportPowerManager>();
			var power = spm.GetPowersForActor(self).FirstOrDefault(sp => !sp.Disabled);

			if (power == null) return 0;

			return 1 - (float)power.RemainingTime / power.TotalTime;
		}

		Color ISelectionBar.GetColor() { return info.Color; }
	}
}
