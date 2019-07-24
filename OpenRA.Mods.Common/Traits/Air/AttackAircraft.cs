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

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	// TODO: Add CurleyShuffle (TD, TS), Circle (Generals Gunship-style)
	public enum AirAttackType { Hover, Strafe }

	public class AttackAircraftInfo : AttackFollowInfo, Requires<AircraftInfo>
	{
		[Desc("Attack behavior. Currently supported types are Strafe (default) and Hover.")]
		public readonly AirAttackType AttackType = AirAttackType.Strafe;

		[Desc("Delay, in game ticks, before strafing aircraft turns to attack.")]
		public readonly int AttackTurnDelay = 50;

		[Desc("Does this actor cancel its attack activity when it needs to resupply? Setting this to 'false' will make the actor resume attack after reloading.")]
		public readonly bool AbortOnResupply = true;

		public override object Create(ActorInitializer init) { return new AttackAircraft(init.Self, this); }
	}

	public class AttackAircraft : AttackFollow
	{
		public new readonly AttackAircraftInfo Info;
		readonly AircraftInfo aircraftInfo;

		public AttackAircraft(Actor self, AttackAircraftInfo info)
			: base(self, info)
		{
			Info = info;
			aircraftInfo = self.Info.TraitInfo<AircraftInfo>();
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor = null)
		{
			return new FlyAttack(self, newTarget, forceAttack, targetLineColor);
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			// Don't fire while landed or when outside the map.
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length < aircraftInfo.MinAirborneAltitude
				|| !self.World.Map.Contains(self.Location))
				return false;

			if (!base.CanAttack(self, target))
				return false;

			return TargetInFiringArc(self, target, base.Info.FacingTolerance);
		}
	}
}
