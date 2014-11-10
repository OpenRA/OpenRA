#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA.Effects
{
	class Rank : IEffect
	{
		readonly Actor self;
		readonly Animation anim;
		readonly string paletteName;

		public Rank(Actor self, string paletteName)
		{
			this.self = self;
			this.paletteName = paletteName;

			var xp = self.Trait<GainsExperience>();
			anim = new Animation(self.World, "rank");
			anim.PlayRepeating("rank");
			anim.PlayFetchIndex("rank", () => xp.Level == 0 ? 0 : xp.Level - 1);
		}

		public void Tick(World world)
		{
			if (self.IsDead)
				world.AddFrameEndTask(w => w.Remove(this));
			else
				anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!self.IsInWorld)
				yield break;

			if (self.IsDead)
				yield break;

			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			if (wr.world.FogObscures(self))
				yield break;

			var pos = wr.ScreenPxPosition(self.CenterPosition);
			var bounds = self.Bounds;
			bounds.Offset(pos.X, pos.Y);

			var palette = wr.Palette(paletteName);
			var offset = (int)(4 / wr.Viewport.Zoom);
			var effectPos = wr.Position(new int2(bounds.Right - offset, bounds.Bottom - offset));
			yield return new SpriteRenderable(anim.Image, effectPos, WVec.Zero, 0, palette, 1f / wr.Viewport.Zoom, true);
		}
	}
}
