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
	class CaptureBuilding : IActivity
	{
		Target target;

		public CaptureBuilding(Actor target) { this.target = Target.FromActor(target); }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (!target.IsValid) return NextActivity;
			if ((target.Actor.Location - self.Location).Length > 1)
				return NextActivity;
			
			self.World.AddFrameEndTask(w =>
			{
				// momentarily remove from world so the ownership queries don't get confused
				var oldOwner = target.Actor.Owner;
				w.Remove(target.Actor);
				target.Actor.Owner = self.Owner;
				w.Add(target.Actor);
				
				foreach (var t in target.Actor.TraitsImplementing<INotifyCapture>())
					t.OnCapture(target.Actor, self, oldOwner, self.Owner);

				self.Destroy();
			});
			return NextActivity;
		}

		public void Cancel(Actor self) { target = Target.None; NextActivity = null; }
	}
}
