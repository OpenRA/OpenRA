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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	public class FrozenActorProxy : IEffect
	{
		readonly Actor self;
		readonly IEnumerable<CPos> footprint;
		IRenderable[] renderables;

		public FrozenActorProxy(Actor self, IEnumerable<CPos> footprint)
		{
			this.self = self;
			this.footprint = footprint;
		}

		public void Tick(World world) { }
		public void SetRenderables(IEnumerable<IRenderable> r)
		{
			renderables = r.Select(rr => rr).ToArray();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (renderables == null)
				return SpriteRenderable.None;

			if (footprint.Any(c => !wr.world.FogObscures(c)))
			{
				if (self.Destroyed)
					self.World.AddFrameEndTask(w => w.Remove(this));

				return SpriteRenderable.None;
			}

			return renderables;
		}
	}
}
