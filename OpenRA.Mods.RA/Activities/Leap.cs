﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Leap : Activity
	{
		Target target;
		PPos initialLocation;

		int moveFraction;
		const int delay = 6;

		public Leap(Actor self, Target target)
		{
			this.target = target;
			initialLocation = (PPos) self.Trait<Mobile>().PxPosition;

			self.Trait<RenderInfantry>().Attacking(self, target);
			Sound.Play("dogg5p.aud", self.CenterLocation);
		}

		public override Activity Tick(Actor self)
		{
			if( moveFraction == 0 && IsCanceled ) return NextActivity;
			if (!target.IsValid) return NextActivity;

			self.Trait<AttackLeap>().IsLeaping = true;
			var mobile = self.Trait<Mobile>();
			++moveFraction;

			mobile.PxPosition = PPos.Lerp(initialLocation, target.PxPosition, moveFraction, delay);

			if (moveFraction >= delay)
			{
				self.TraitsImplementing<IMove>().FirstOrDefault()
					.SetPosition(self, target.CenterLocation.ToCPos());

				if (target.IsActor)
					target.Actor.Kill(self);
				self.Trait<AttackLeap>().IsLeaping = false;
				return NextActivity;
			}

			return this;
		}
	}
}
