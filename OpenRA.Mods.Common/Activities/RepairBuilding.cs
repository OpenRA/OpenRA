#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class RepairBuilding : Enter
	{
		readonly EngineerRepairInfo info;

		Actor enterActor;
		IHealth enterHealth;

		public RepairBuilding(Actor self, Target target, EngineerRepairInfo info)
			: base(self, target, Color.Yellow)
		{
			this.info = info;
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			enterActor = targetActor;
			enterHealth = targetActor.TraitOrDefault<IHealth>();

			// Make sure we can still repair the target before entering
			// (but not before, because this may stop the actor in the middle of nowhere)
			var stance = self.Owner.Stances[enterActor.Owner];
			if (enterHealth == null || enterHealth.DamageState == DamageState.Undamaged || !info.ValidStances.HasStance(stance))
			{
				Cancel(self, true);
				return false;
			}

			return true;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			// Make sure the target hasn't changed while entering
			// OnEnterComplete is only called if targetActor is alive
			if (targetActor != enterActor)
				return;

			if (enterHealth.DamageState == DamageState.Undamaged)
				return;

			var stance = self.Owner.Stances[enterActor.Owner];
			if (!info.ValidStances.HasStance(stance))
				return;

			if (enterHealth.DamageState == DamageState.Undamaged)
				return;

			enterActor.InflictDamage(self, new Damage(-enterHealth.MaxHP));
			if (!string.IsNullOrEmpty(info.RepairSound))
				Game.Sound.Play(SoundType.World, info.RepairSound, enterActor.CenterPosition);

			if (info.EnterBehaviour == EnterBehaviour.Dispose)
				self.Dispose();
			else if (info.EnterBehaviour == EnterBehaviour.Suicide)
				self.Kill(self);
		}
	}
}
