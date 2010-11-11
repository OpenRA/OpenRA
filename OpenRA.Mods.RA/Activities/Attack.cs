#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Activities
{
	/* non-turreted attack */
	public class Attack : CancelableActivity
	{
		Target Target;
		int Range;
		bool AllowMovement;

		public Attack(Target target, int range, bool allowMovement)
		{
			Target = target;
			Range = range;
			AllowMovement = allowMovement;
		}

		public Attack(Target target, int range) : this(target, range, true)
		{
			
		}

		public override IActivity Tick( Actor self )
		{
			var attack = self.Trait<AttackBase>();

			var ret = InnerTick( self, attack );
			attack.IsAttacking = ( ret == this );
			return ret;
		}

		IActivity InnerTick( Actor self, AttackBase attack )
		{
			if (IsCanceled) return NextActivity;
			var facing = self.Trait<IFacing>();
			if (!Target.IsValid)
				return NextActivity;

			if (!Combat.IsInRange(self.CenterLocation, Range, Target))
				return (AllowMovement) ? Util.SequenceActivities(self.Trait<Mobile>().MoveWithinRange(Target, Range), this) : NextActivity;

			var desiredFacing = Util.GetFacing(Target.CenterLocation - self.CenterLocation, 0);
			var renderUnit = self.TraitOrDefault<RenderUnit>();
			var numDirs = (renderUnit != null)
				? renderUnit.anim.CurrentSequence.Facings : 8;

			if (facing.Facing != desiredFacing)
				return Util.SequenceActivities( new Turn( desiredFacing ), this );

			attack.target = Target;
			attack.DoAttack(self, Target);
			return this;
		}
	}
}
