#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
	class Rank : IEffect
	{
		Actor self;
		Animation anim = new Animation("rank");

		public Rank(Actor self)
		{
			this.self = self;
			var xp = self.Trait<GainsExperience>();

			anim.PlayRepeating("rank");
			anim.PlayFetchIndex("rank", () => xp.Level == 0 ? 0 : xp.Level - 1);
		}

		public void Tick(World world)
		{
			if (self.IsDead())
				world.AddFrameEndTask(w => w.Remove(this));
			else
				anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!self.IsInWorld)
				yield break;

			if (self.IsDead())
				yield break;

			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			if (wr.world.FogObscures(self))
				yield break;

			var pos = wr.ScreenPxPosition(self.CenterPosition);
			var bounds = self.Bounds.Value;
			bounds.Offset(pos.X, pos.Y);

			var palette = wr.Palette("rank");
			var offset = (int)(4 / wr.Viewport.Zoom);
			var effectPos = wr.Position(new int2(bounds.Right - offset, bounds.Bottom - offset));
			yield return new SpriteRenderable(anim.Image, effectPos, WVec.Zero, 0, palette, 1f / wr.Viewport.Zoom, true);
		}
	}
}
