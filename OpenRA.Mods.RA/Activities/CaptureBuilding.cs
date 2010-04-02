#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class CaptureBuilding : IActivity
	{
		Actor target;

		public CaptureBuilding(Actor target) { this.target = target; }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead) return NextActivity;

			if (self.Owner.Stances[ target.Owner ] == Stance.Ally)
			{
				if (target.Health == target.Info.Traits.Get<OwnedActorInfo>().HP)
					return NextActivity;
				target.InflictDamage(self, -EngineerCapture.EngineerDamage, null);
			}
			else
			{
				if (target.Health - EngineerCapture.EngineerDamage <= 0)
				{
					target.Owner = self.Owner;
					target.InflictDamage(self, target.Health - EngineerCapture.EngineerDamage, null);
				}
				else
					target.InflictDamage(self, EngineerCapture.EngineerDamage, null);
			}

			// the engineer is sacrificed.
			self.World.AddFrameEndTask(w => w.Remove(self));

			return NextActivity;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
