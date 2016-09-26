#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
		readonly Aircraft helicopter;
		readonly AttackHeli attackHeli;
		readonly AmmoPool[] ammoPools;
		readonly bool attackOnlyVisibleTargets;

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
			helicopter = self.Trait<Aircraft>();
			attackHeli = self.Trait<AttackHeli>();
			ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
			this.attackOnlyVisibleTargets = attackOnlyVisibleTargets;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			if (attackOnlyVisibleTargets && target.Type == TargetType.Actor && canHideUnderFog
				&& !self.Owner.CanTargetActor(target.Actor))
			{
				var newTarget = Target.FromCell(self.World, self.World.Map.CellContaining(target.CenterPosition));

				Cancel(self);
				self.SetTargetLine(newTarget, Color.Green);
				return new HeliFly(self, newTarget);
			}

			// If any AmmoPool is depleted and no weapon is valid against target, return to helipad to reload and then resume the activity
			if (ammoPools.Any(x => !x.Info.SelfReloads && !x.HasAmmo()) && !attackHeli.HasAnyValidWeapons(target))
				return ActivityUtils.SequenceActivities(new HeliReturnToBase(self, helicopter.Info.AbortOnResupply), this);

			var dist = target.CenterPosition - self.CenterPosition;

			// Can rotate facing while ascending
			var desiredFacing = dist.HorizontalLengthSquared != 0 ? dist.Yaw.Facing : helicopter.Facing;
			helicopter.Facing = Util.TickFacing(helicopter.Facing, desiredFacing, helicopter.TurnSpeed);

			if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.CruiseAltitude))
				return this;

			// Fly towards the target
			if (!target.IsInRange(self.CenterPosition, attackHeli.GetMaximumRangeVersusTarget(target)))
				helicopter.SetPosition(self, helicopter.CenterPosition + helicopter.FlyStep(desiredFacing));

			// Fly backwards from the target
			if (target.IsInRange(self.CenterPosition, attackHeli.GetMinimumRangeVersusTarget(target)))
			{
				// Facing 0 doesn't work with the following position change
				var facing = 1;
				if (desiredFacing != 0)
					facing = desiredFacing;
				else if (helicopter.Facing != 0)
					facing = helicopter.Facing;
				helicopter.SetPosition(self, helicopter.CenterPosition + helicopter.FlyStep(-facing));
			}

			attackHeli.DoAttack(self, target);

			return this;
		}
	}
}
