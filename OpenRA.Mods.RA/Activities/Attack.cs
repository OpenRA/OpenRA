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

namespace OpenRA.Mods.RA.Activities
{
	/* non-turreted attack */
	public class Attack : IActivity
	{
		Target Target;
		int Range;

		public Attack(Target target, int range)
		{
			Target = target;
			Range = range;
		}

		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			var unit = self.traits.Get<Unit>();

			if (!Target.IsValid)
				return NextActivity;

			var targetCell = Util.CellContaining(Target.CenterLocation);

			if ((targetCell - self.Location).LengthSquared >= Range * Range)
				return new Move( Target, Range ) { NextActivity = this };

			var desiredFacing = Util.GetFacing((targetCell - self.Location).ToFloat2(), 0);
			var renderUnit = self.traits.GetOrDefault<RenderUnit>();
			var numDirs = (renderUnit != null)
				? renderUnit.anim.CurrentSequence.Facings : 8;

			if (Util.QuantizeFacing(unit.Facing, numDirs) 
				!= Util.QuantizeFacing(desiredFacing, numDirs))
			{
				return new Turn( desiredFacing ) { NextActivity = this };
			}

			var attack = self.traits.Get<AttackBase>();
			attack.target = Target;
			attack.DoAttack(self);
			return this;
		}

		public void Cancel(Actor self) { Target = Target.None; }
	}
}
