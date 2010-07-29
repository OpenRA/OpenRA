#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Infiltrate : IActivity
	{
		Actor target;
		public Infiltrate(Actor target) { this.target = target; }
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead()) return NextActivity;
			if (target.Owner == self.Owner) return NextActivity;

			foreach (var t in target.traits.WithInterface<IAcceptSpy>())
				t.OnInfiltrate(target, self);

			self.World.AddFrameEndTask(w => w.Remove(self));

			return NextActivity;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
