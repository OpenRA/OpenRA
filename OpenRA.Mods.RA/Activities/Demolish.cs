#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Demolish : CancelableActivity
	{
		Target target;

		public Demolish( Actor target ) { this.target = Target.FromActor(target); }

		public override IActivity Tick(Actor self)
		{
			if( IsCanceled ) return NextActivity;
			if (!target.IsValid) return NextActivity;
			if ((target.Actor.Location - self.Location).Length > 1)
				return NextActivity;


			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(25 * 2,
				() => { if (target.IsValid) target.Actor.Kill(self); })));
			return NextActivity;
		}
	}
}
