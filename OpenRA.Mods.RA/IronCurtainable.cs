#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class IronCurtainableInfo : TraitInfo<IronCurtainable> { }

	class IronCurtainable : IDamageModifier, ITick
	{
		[Sync]
		int RemainingTicks = 0;

		public void Tick(Actor self)
		{
			if (RemainingTicks > 0)
				RemainingTicks--;
		}

        public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			return (RemainingTicks > 0) ? 0.0f : 1.0f;
		}

		public void Activate(Actor self, int duration)
		{
			if (RemainingTicks == 0)
				self.World.AddFrameEndTask(w => w.Add(new InvulnEffect(self))); // do not stack the invuln effect

			RemainingTicks = duration;
		}
	}
}