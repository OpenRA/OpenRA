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
	[Desc("SOON TO BE DEPRECATED. This actor can be sent near a structure for repairs.")]
	class RepairableNearInfo : ITraitInfo, Requires<IHealthInfo>
	{
		[FieldLoader.Require]
		[ActorReference] public readonly HashSet<string> RepairActors = new HashSet<string> { };

		public readonly WDist CloseEnough = WDist.FromCells(4);

		public object Create(ActorInitializer init) { return new RepairableNear(init.Self, this); }
	}

	class RepairableNear : IResupplyable
	{
		public readonly RepairableNearInfo Info;
		readonly IHealth health;

		public RepairableNear(Actor self, RepairableNearInfo info)
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

		WDist IResupplyable.CloseEnough { get { return Info.CloseEnough; } }
	}
}
