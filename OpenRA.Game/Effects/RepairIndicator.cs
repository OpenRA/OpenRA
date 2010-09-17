#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Effects
{
	class RepairIndicator : IEffect
	{
		int framesLeft;
		Actor a;
		Animation anim = new Animation("select");

		public RepairIndicator(Actor a) 
		{ 
			this.a = a; anim.PlayRepeating("repair"); 
			framesLeft =  (int)(a.Info.Traits.Get<RepairableBuildingInfo>().RepairRate * 25 * 60 / 2);
		}

		public void Tick( World world )
		{
			if (--framesLeft == 0 || a.IsDead())
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, 
				a.CenterLocation - .5f * anim.Image.size, "chrome", (int)a.CenterLocation.Y);
		}
	}
}
