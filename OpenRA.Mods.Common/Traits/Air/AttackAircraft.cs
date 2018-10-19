#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class AttackAircraftInfo : AttackFrontalInfo, Requires<AircraftInfo>
	{
		[Desc("Delay, in game ticks, before non-hovering aircraft turns to attack.")]
		public readonly int AttackTurnDelay = 50;

		public override object Create(ActorInitializer init) { return new AttackAircraft(init.Self, this); }
	}

	public class AttackAircraft : AttackFrontal
	{
		public readonly AttackAircraftInfo AttackAircraftInfo;
		readonly AircraftInfo aircraftInfo;

		public AttackAircraft(Actor self, AttackAircraftInfo info)
			: base(self, info)
		{
			AttackAircraftInfo = info;
			aircraftInfo = self.Info.TraitInfo<AircraftInfo>();
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			if (aircraftInfo.CanHover)
				return new HeliAttack(self, newTarget);

			return new FlyAttack(self, newTarget);
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			// Don't fire while landed or when outside the map.
			return base.CanAttack(self, target)
				&& self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length >= aircraftInfo.MinAirborneAltitude
				&& self.World.Map.Contains(self.Location);
		}
	}
}
