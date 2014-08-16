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
using OpenRA.GameRules;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class IronCurtainableInfo : TraitInfo<IronCurtainable> { }

	class IronCurtainable : IDamageModifier, ITick, ISync, ISelectionBar
	{
		[Sync] int RemainingTicks = 0;
		int TotalTicks;

		public void Tick(Actor self)
		{
			if (RemainingTicks > 0)
				RemainingTicks--;
		}

		public int GetDamageModifier(Actor attacker, DamageWarhead warhead)
		{
			return RemainingTicks > 0 ? 0 : 100;
		}

		public void Activate(Actor self, int duration)
		{
			if (RemainingTicks == 0)
				self.World.AddFrameEndTask(w => w.Add(new InvulnEffect(self))); // do not stack the invuln effect

			RemainingTicks = duration;
			TotalTicks = duration;
		}

		// Show the remaining time as a bar
		public float GetValue()
		{
			if (RemainingTicks == 0) // otherwise an empty bar is rendered all the time
				return 0f;

			return (float)RemainingTicks / TotalTicks;
		}
		public Color GetColor() { return Color.Red; }
	}
}