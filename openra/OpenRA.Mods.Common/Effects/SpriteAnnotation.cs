#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Effects
{
	public class SpriteAnnotation : IEffect, IEffectAnnotation
	{
		readonly string palette;
		readonly Animation anim;
		readonly WPos pos;

		public SpriteAnnotation(WPos pos, World world, string image, string sequence, string palette)
		{
			this.palette = palette;
			this.pos = pos;
			anim = new Animation(world, image);
			anim.PlayThen(sequence, () => world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); }));
			world.ScreenMap.Add(this, pos, anim.Image);
		}

		void IEffect.Tick(World world)
		{
			anim.Tick();
			world.ScreenMap.Update(this, pos, anim.Image);
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr) { yield break; }

		IEnumerable<IRenderable> IEffectAnnotation.RenderAnnotation(WorldRenderer wr)
		{
			var screenPos = wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(pos));
			return anim.RenderUI(wr, screenPos, WVec.Zero, 0, wr.Palette(palette));
		}
	}
}
