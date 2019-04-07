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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be sent to a structure for repairs.")]
	public class RepairableInfo : ITraitInfo, Requires<IHealthInfo>
	{
		[FieldLoader.Require]
		[ActorReference] public readonly HashSet<string> RepairActors = new HashSet<string> { };

		[Desc("The amount the unit will be repaired at each step. Use -1 for fallback behavior where HpPerStep from RepairsUnits trait will be used.")]
		public readonly int HpPerStep = -1;

		public virtual object Create(ActorInitializer init) { return new Repairable(init.Self, this); }
	}

	public class Repairable : IResupplyable
	{
		public readonly RepairableInfo Info;
		readonly IHealth health;

		public Repairable(Actor self, RepairableInfo info)
		{
			Info = info;
			health = self.Trait<IHealth>();
		}

		bool CanRepairAt(Actor target)
		{
			return Info.RepairActors.Contains(target.Info.Name);
		}

		bool IResupplyable.CanResupplyAt(Actor target)
		{
			return CanRepairAt(target);
		}

		bool IResupplyable.NeedsResupplyAt(Actor target)
		{
			return CanRepairAt(target) && health.DamageState > DamageState.Undamaged;
		}

		WDist IResupplyable.CloseEnough { get { return new WDist(512); } }
	}
}
