#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyAttack : Activity
	{
		readonly AttackPlane attackPlane;
		readonly IEnumerable<AmmoPool> ammoPools;

		Target target;
		bool isFrozenUnderFog;
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
					isFrozenUnderFog = target.Actor.HasTrait<FrozenUnderFog>();
			}
		}

		Activity inner;
		int ticksUntilTurn;

		public FlyAttack(Actor self, Target target)
		{
			this.target = target;
			attackPlane = self.TraitOrDefault<AttackPlane>();
			ammoPools = self.TraitsImplementing<AmmoPool>();
			ticksUntilTurn = attackPlane.AttackPlaneInfo.AttackTurnDelay;
		}

		public override Activity Tick(Actor self)
		{
			if (!target.IsValidFor(self))
				return NextActivity;

			if (target.Type == TargetType.Actor && !isFrozenUnderFog
				 && !self.Owner.Shroud.IsTargetable(target.Actor))
			{
				var newTarget = Target.FromCell(self.World, self.World.Map.CellContaining(target.CenterPosition));

				self.CancelActivity();
				self.SetTargetLine(newTarget, Color.Green);
				return Util.SequenceActivities(new Fly(self, newTarget), new FlyCircle(self));
			}

			// Move to the next activity only if all ammo pools are depleted and none reload automatically
			// TODO: This should check whether there is ammo left that is actually suitable for the target
			if (ammoPools != null && ammoPools.All(x => !x.Info.SelfReloads && !x.HasAmmo()))
				return NextActivity;

			if (attackPlane != null)
				attackPlane.DoAttack(self, target);

			if (inner == null)
			{
				if (IsCanceled)
					return NextActivity;

				// TODO: This should fire each weapon at its maximum range
				if (target.IsInRange(self.CenterPosition, attackPlane.Armaments.Select(a => a.Weapon.MinRange).Min()))
					inner = Util.SequenceActivities(new FlyTimed(ticksUntilTurn, self), new Fly(self, target), new FlyTimed(ticksUntilTurn, self));
				else
					inner = Util.SequenceActivities(new Fly(self, target), new FlyTimed(ticksUntilTurn, self));
			}

			inner = Util.RunActivity(self, inner);

			return this;
		}

		public override void Cancel(Actor self)
		{
			if (!IsCanceled && inner != null)
				inner.Cancel(self);

			// NextActivity must always be set to null:
			base.Cancel(self);
		}
	}
}
