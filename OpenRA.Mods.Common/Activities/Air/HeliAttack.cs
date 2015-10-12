#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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

				self.CancelActivity();
				self.SetTargetLine(newTarget, Color.Green);
				return Util.SequenceActivities(new HeliFly(self, newTarget));
			}

			// If all ammo pools are depleted and none reload automatically, return to helipad to reload and then move to next activity
			// TODO: This should check whether there is ammo left that is actually suitable for the target
			if (ammoPools.All(x => !x.Info.SelfReloads && !x.HasAmmo()))
				return Util.SequenceActivities(new HeliReturnToBase(self), NextActivity);

			var dist = target.CenterPosition - self.CenterPosition;

			// Can rotate facing while ascending
			var desiredFacing = Util.GetFacing(dist, helicopter.Facing);
			helicopter.Facing = Util.TickFacing(helicopter.Facing, desiredFacing, helicopter.ROT);

			if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.CruiseAltitude))
				return this;

			// Fly towards the target
			// TODO: Fix that the helicopter won't do anything if it has multiple weapons with different ranges
			// and the weapon with the longest range is out of ammo
			if (!target.IsInRange(self.CenterPosition, attackHeli.GetMaximumRange()))
				helicopter.SetPosition(self, helicopter.CenterPosition + helicopter.FlyStep(desiredFacing));

			// Fly backwards from the target
			// TODO: Same problem as with MaximumRange
			if (target.IsInRange(self.CenterPosition, attackHeli.GetMinimumRange()))
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
