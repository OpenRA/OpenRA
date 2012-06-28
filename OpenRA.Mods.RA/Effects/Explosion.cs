﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	public class Explosion : IEffect
	{
		Animation anim;
		PPos pos;
		int altitude;

		public Explosion(World world, PPos pixelPos, string style, bool isWater, int altitude)
		{
			this.pos = pixelPos;
			this.altitude = altitude;
			anim = new Animation("explosion");
			anim.PlayThen(style,
				() => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world ) { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,
				pos.ToFloat2() - .5f * anim.Image.size - new int2(0,altitude),
				"effect", (int)pos.Y - altitude);
		}

		public Player Owner { get { return null; } }
	}
}
