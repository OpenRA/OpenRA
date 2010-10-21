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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Infiltrate : CancelableActivity
	{
		Actor target;
		public Infiltrate(Actor target) { this.target = target; }

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (target == null || !target.IsInWorld || target.IsDead()) return NextActivity;
			if (target.Owner == self.Owner) return NextActivity;

			if( !target.Trait<IOccupySpace>().OccupiedCells().Any( x => x == self.Location ) )
				return NextActivity;

			foreach (var t in target.TraitsImplementing<IAcceptSpy>())
				t.OnInfiltrate(target, self);

			self.Destroy();

			return this;
		}
	}
}
