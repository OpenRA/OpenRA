#region Copyright & License Information
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
	public class Corpse : IEffect
	{
		readonly Animation Anim;
		readonly WPos Pos;
		readonly string PaletteName;

		public Corpse(World world, WPos pos, string image, string sequence, string paletteName)
		{
			Pos = pos;
			PaletteName = paletteName;
			Anim = new Animation(image);
			Anim.PlayThen(sequence, () => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick(World world) { Anim.Tick(); }

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield return new SpriteRenderable(Anim.Image, Pos, 0, wr.Palette(PaletteName), 1f);
		}
	}
}
