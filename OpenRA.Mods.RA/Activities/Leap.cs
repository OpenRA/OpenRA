#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Leap : IActivity
	{
		Target target;
		float2 initialLocation;
		float t;

		const int delay = 6;

		public Leap(Actor self, Target target)
		{
			this.target = target; 
			initialLocation = self.CenterLocation;

			self.traits.Get<RenderInfantry>().Attacking(self);
			Sound.Play("dogg5p.aud", self.CenterLocation);
		}

		public IActivity NextActivity { get; set; }
		
		public IActivity Tick(Actor self)
		{
			if (!target.IsValid)
				return NextActivity;

			t += (1f / delay);

			self.CenterLocation = float2.Lerp(initialLocation, target.CenterLocation, t);

			if (t >= 1f)
			{
				self.traits.WithInterface<IMove>().FirstOrDefault()
					.SetPosition(self, Util.CellContaining(target.CenterLocation));

				if (target.IsActor)
					target.Actor.InflictDamage(self, target.Actor.Health, null);	// kill it
				return NextActivity;
			}

			return this;
		}

		public void Cancel(Actor self) { target = new Target(); NextActivity = null; }
	}
}
