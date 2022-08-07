#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be sent to a structure for repairs.")]
	public class RepairableInfo : DockableInfo, Requires<IHealthInfo>
	{
		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("The amount the unit will be repaired at each step. Use -1 for fallback behavior where HpPerStep from RepairsUnits trait will be used.")]
		public readonly int HpPerStep = -1;

		[Desc("Repair dock types")]
		public readonly BitSet<DockType> DockType = new BitSet<DockType>("repair");

		public override object Create(ActorInitializer init) { return new Repairable(init.Self, this); }
	}

	public class Repairable : Dockable<RepairableInfo>, INotifyOwnerChanged
	{
		readonly IHealth health;
		readonly int unitCost;
		PlayerResources playerResources;

		protected override BitSet<DockType> DockType() { return Info.DockType; }

		public Repairable(Actor self, RepairableInfo info)
			: base(self, info)
		{
			health = self.Trait<IHealth>();
			unitCost = self.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		public override bool CanDock()
		{
			return health.DamageState > DamageState.Undamaged;
		}

		RepairsUnits[] allRepairsUnits;
		int remainingTicks;

		public override void DockStarted(Dock dock)
		{
			allRepairsUnits = dock.Self.TraitsImplementing<RepairsUnits>().ToArray();
		}

		public override bool TickDock(Dock dock)
		{
			var repairsUnits = allRepairsUnits.FirstOrDefault(r => !r.IsTraitDisabled && !r.IsTraitPaused);
			if (repairsUnits == null)
				return true;

			if (health.DamageState == DamageState.Undamaged)
			{
				// Give experience to the allied dock owner
				if (dock.Self.Owner != Self.Owner)
					dock.Self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()?.GiveExperience(repairsUnits.Info.PlayerExperience);

				return true;
			}

			if (remainingTicks == 0)
			{
				var hpToRepair = Info.HpPerStep > 0 ? Info.HpPerStep : repairsUnits.Info.HpPerStep;

				// Cast to long to avoid overflow when multiplying by the health
				var value = (long)unitCost * repairsUnits.Info.ValuePercentage;
				var cost = value == 0 ? 0 : Math.Max(1, (int)(hpToRepair * value / (health.MaxHP * 100L)));

				if (!playerResources.TakeCash(cost, true))
				{
					remainingTicks = 1;
					return false;
				}

				Self.InflictDamage(dock.Self, new Damage(-hpToRepair, repairsUnits.Info.RepairDamageTypes));
				remainingTicks = repairsUnits.Info.Interval;
			}
			else
				--remainingTicks;

			return false;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}
	}
}
