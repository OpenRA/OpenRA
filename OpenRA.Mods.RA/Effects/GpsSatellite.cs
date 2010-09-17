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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class GpsSatellite : IEffect
	{
		readonly float heightPerTick = 10;
		float2 offset;
		Animation anim = new Animation("sputnik");

		public GpsSatellite(float2 offset)
		{
			this.offset = offset;
			anim.PlayRepeating("idle");
		}

		public void Tick( World world )
		{
			anim.Tick();
			offset.Y -= heightPerTick;
			
			if (offset.Y < 0)
				world.AddFrameEndTask(w => w.Remove(this));
		}
		
		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,offset, "effect", (int)offset.Y);
		}
	}
}
