#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	/* non-turreted attack */
	public class Attack : Activity
	{
		protected readonly Target Target;
		readonly AttackBase attack;
		readonly IMove move;
		readonly IFacing facing;
		readonly IPositionable positionable;
		readonly bool forceAttack;

		WDist minRange;
		WDist maxRange;

		public Attack(Actor self, Target target, bool allowMovement, bool forceAttack)
		{
			Target = target;

			this.forceAttack = forceAttack;

			attack = self.Trait<AttackBase>();
			facing = self.Trait<IFacing>();
			positionable = self.Trait<IPositionable>();

			move = allowMovement ? self.TraitOrDefault<IMove>() : null;
		}

		public override Activity Tick(Actor self)
		{
			var ret = InnerTick(self, attack);
			attack.IsAttacking = ret == this;
			return ret;
		}

		protected virtual Activity InnerTick(Actor self, AttackBase attack)
		{
			if (IsCanceled)
				return NextActivity;

			var type = Target.Type;
			if (!Target.IsValidFor(self) || type == TargetType.FrozenActor)
				return NextActivity;

			if (attack.Info.AttackRequiresEnteringCell && !positionable.CanEnterCell(Target.Actor.Location, null, false))
				return NextActivity;

			// Drop the target if it moves under the shroud / fog.
			// HACK: This would otherwise break targeting frozen actors
			// The problem is that Shroud.IsTargetable returns false (as it should) for
			// frozen actors, but we do want to explicitly target the underlying actor here.
			if (!attack.Info.IgnoresVisibility && type == TargetType.Actor && !Target.Actor.Info.HasTraitInfo<FrozenUnderFogInfo>() && !self.Owner.CanTargetActor(Target.Actor))
				return NextActivity;

			// Drop the target once none of the weapons are effective against it
			var armaments = attack.ChooseArmamentsForTarget(Target, forceAttack).ToList();
			if (armaments.Count == 0)
				return NextActivity;

			// Update ranges
			minRange = armaments.Max(a => a.Weapon.MinRange);
			maxRange = armaments.Min(a => a.MaxRange());

			// Try to move within range
			if (move != null && (!Target.IsInRange(self.CenterPosition, maxRange) || Target.IsInRange(self.CenterPosition, minRange)))
				return Util.SequenceActivities(move.MoveWithinRange(Target, minRange, maxRange), this);

			var desiredFacing = Util.GetFacing(Target.CenterPosition - self.CenterPosition, 0);
			if (facing.Facing != desiredFacing)
				return Util.SequenceActivities(new Turn(self, desiredFacing), this);

			attack.DoAttack(self, Target, armaments);

			return this;
		}
	}
}
