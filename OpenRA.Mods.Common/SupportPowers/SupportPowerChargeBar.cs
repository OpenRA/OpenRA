#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class SupportPowerChargeBarInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new SupportPowerChargeBar(init.self); }
	}

	class SupportPowerChargeBar : ISelectionBar
	{
		Actor self;
		public SupportPowerChargeBar(Actor self) { this.self = self; }

		public float GetValue()
		{
			// only people we like should see our charge status.
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			var spm = self.Owner.PlayerActor.Trait<SupportPowerManager>();
			var power = spm.GetPowersForActor(self).FirstOrDefault(sp => !sp.Disabled);

			if (power == null) return 0;

			return 1 - (float)power.RemainingTime / power.TotalTime;
		}

		public Color GetColor() { return Color.Magenta; }
	}
}
