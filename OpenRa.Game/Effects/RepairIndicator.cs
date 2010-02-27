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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Effects
{
	class RepairIndicator : IEffect
	{
		int framesLeft = (int)(Rules.General.RepairRate * 25 * 60 / 2);
		Actor a;
		Animation anim = new Animation("select");

		public RepairIndicator(Actor a) { this.a = a; anim.PlayRepeating("repair"); }

		public void Tick( World world )
		{
			if (--framesLeft == 0 || a.IsDead)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, 
				a.CenterLocation - .5f * anim.Image.size, "chrome");
		}
	}
}
