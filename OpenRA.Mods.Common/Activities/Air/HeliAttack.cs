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

using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliAttack : Activity
	{
		readonly Aircraft aircraft;
		readonly AttackAircraft attackAircraft;
		readonly bool attackOnlyVisibleTargets;
		readonly Rearmable rearmable;

		Target target;
		bool canHideUnderFog;
		protected Target Target
		{
			get
			{
				return target;
			}

			private set
			{
				target = value;
				if (target.Type == TargetType.Actor)
					canHideUnderFog = target.Actor.Info.HasTraitInfo<HiddenUnderFogInfo>();
			}
		}

		public HeliAttack(Actor self, Target target, bool attackOnlyVisibleTargets = true)
		{
			Target = target;
			aircraft = self.Trait<Aircraft>();
			attackAircraft = self.Trait<AttackAircraft>();
			this.attackOnlyVisibleTargets = attackOnlyVisibleTargets;
			rearmable = self.TraitOrDefault<Rearmable>();
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			var pos = self.CenterPosition;
			var targetPos = attackAircraft.GetTargetPosition(pos, target);
			if (attackOnlyVisibleTargets && target.Type == TargetType.Actor && canHideUnderFog
				&& !target.Actor.CanBeViewedByPlayer(self.Owner))
			{
				var newTarget = Target.FromCell(self.World, self.World.Map.CellContaining(targetPos));

				Cancel(self);
				self.SetTargetLine(newTarget, Color.Green);
				return new HeliFly(self, newTarget);
			}

			// If all valid weapons have depleted their ammo and Rearmable trait exists, return to RearmActor to reload and then resume the activity
			if (rearmable != null && attackAircraft.Armaments.All(x => x.IsTraitPaused || !x.Weapon.IsValidAgainst(target, self.World, self)))
				return ActivityUtils.SequenceActivities(new HeliReturnToBase(self, aircraft.Info.AbortOnResupply), this);

			var dist = targetPos - pos;

			// Can rotate facing while ascending
			var desiredFacing = dist.HorizontalLengthSquared != 0 ? dist.Yaw.Facing : aircraft.Facing;
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);

			if (HeliFly.AdjustAltitude(self, aircraft, aircraft.Info.CruiseAltitude))
				return this;

			// Fly towards the target
			if (!target.IsInRange(pos, attackAircraft.GetMaximumRangeVersusTarget(target)))
				aircraft.SetPosition(self, aircraft.CenterPosition + aircraft.FlyStep(desiredFacing));

			// Fly backwards from the target
			if (target.IsInRange(pos, attackAircraft.GetMinimumRangeVersusTarget(target)))
			{
				// Facing 0 doesn't work with the following position change
				var facing = 1;
				if (desiredFacing != 0)
					facing = desiredFacing;
				else if (aircraft.Facing != 0)
					facing = aircraft.Facing;
				aircraft.SetPosition(self, aircraft.CenterPosition + aircraft.FlyStep(-facing));
			}

			attackAircraft.DoAttack(self, target);

			return this;
		}
	}
}
