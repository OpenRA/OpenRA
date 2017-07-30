#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class RepairBuilding : Enter
	{
		readonly Actor target;
		readonly Health health;
		readonly EngineerRepairInfo repairInfo;
		readonly ExternalCondition[] externalConditions;

		public RepairBuilding(Actor self, Actor target, EngineerRepairInfo repairInfo)
			: base(self, target, repairInfo.EnterBehaviour, WDist.Zero)
		{
			this.target = target;
			this.repairInfo = repairInfo;
			health = target.Trait<Health>();
			externalConditions = target.TraitsImplementing<ExternalCondition>()
				.Where(ec => repairInfo.RevokeExternalConditions.Contains(ec.Info.Condition)).ToArray();
		}

		protected override bool CanReserve(Actor self)
		{
			return health.DamageState != DamageState.Undamaged;
		}

		protected override void OnInside(Actor self)
		{
			var stance = self.Owner.Stances[target.Owner];
			if (!stance.HasStance(repairInfo.ValidStances))
				return;

			if (health.DamageState == DamageState.Undamaged)
				return;

			foreach (var ec in externalConditions)
				ec.TryRevokeCondition(target);

			target.InflictDamage(self, new Damage(-health.MaxHP));
		}
	}
}
