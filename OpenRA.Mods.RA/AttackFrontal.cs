#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	abstract class AttackFrontal : AttackBase
	{
		public AttackFrontal(Actor self, int facingTolerance)
			: base(self) { FacingTolerance = facingTolerance; }

		readonly int FacingTolerance;

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (!target.IsValid) return;

			var move = self.traits.Get<IMove>();
			var facingToTarget = Util.GetFacing(target.CenterLocation - self.CenterLocation, move.Facing);

			if (Math.Abs(facingToTarget - move.Facing) % 256 < FacingTolerance)
				DoAttack(self);
		}
	}
}
