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
		readonly bool attackOnlyVisibleTargets;
		readonly bool autoReloads;

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
			this.attackOnlyVisibleTargets = attackOnlyVisibleTargets;
			autoReloads = self.TraitsImplementing<AmmoPool>().All(p => p.AutoReloads);
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (helicopter.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			var pos = self.CenterPosition;
			var targetPos = attackHeli.GetTargetPosition(pos, target);
			if (attackOnlyVisibleTargets && target.Type == TargetType.Actor && canHideUnderFog
				&& !target.Actor.CanBeViewedByPlayer(self.Owner))
			{
				var newTarget = Target.FromCell(self.World, self.World.Map.CellContaining(targetPos));

				Cancel(self);
				self.SetTargetLine(newTarget, Color.Green);
				return new HeliFly(self, newTarget);
			}

			// If all valid weapons have depleted their ammo and RearmBuilding is defined, return to RearmBuilding to reload and then resume the activity
			if (!autoReloads && helicopter.Info.RearmBuildings.Any() && attackHeli.Armaments.All(x => x.IsTraitPaused || !x.Weapon.IsValidAgainst(target, self.World, self)))
				return ActivityUtils.SequenceActivities(new HeliReturnToBase(self, helicopter.Info.AbortOnResupply), this);

			var dist = targetPos - pos;

			// Can rotate facing while ascending
			var desiredFacing = dist.HorizontalLengthSquared != 0 ? dist.Yaw.Facing : helicopter.Facing;
			helicopter.Facing = Util.TickFacing(helicopter.Facing, desiredFacing, helicopter.TurnSpeed);

			if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.CruiseAltitude))
				return this;

			// Fly towards the target
			if (!target.IsInRange(pos, attackHeli.GetMaximumRangeVersusTarget(target)))
				helicopter.SetPosition(self, helicopter.CenterPosition + helicopter.FlyStep(desiredFacing));

			// Fly backwards from the target
			if (target.IsInRange(pos, attackHeli.GetMinimumRangeVersusTarget(target)))
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
