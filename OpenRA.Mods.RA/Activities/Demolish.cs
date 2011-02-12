#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	class Demolish : CancelableActivity
	{
		Actor target;

		public Demolish( Actor target ) { this.target = target; }

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (target == null || !target.IsInWorld || target.IsDead()) return NextActivity;
			
			if( !target.Trait<IOccupySpace>().OccupiedCells().Any( x => x.First == self.Location ) )
				return NextActivity;

			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(25 * 2,
				() => { if (target.IsInWorld) target.Kill(self); })));
			return NextActivity;
		}
	}
}
