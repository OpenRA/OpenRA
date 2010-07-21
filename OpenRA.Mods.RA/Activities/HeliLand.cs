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
	class HeliLand : IActivity
	{
		public HeliLand(bool requireSpace) { this.requireSpace = requireSpace; }

		bool requireSpace;
		bool isCanceled;
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			var unit = self.traits.Get<Unit>();
			if (unit.Altitude == 0)
				return NextActivity;

			if (requireSpace && !self.traits.Get<IMove>().CanEnterCell(self.Location))
				return this;
			
			--unit.Altitude;
			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
