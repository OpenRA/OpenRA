#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class RenderShroudCircleInfo : ITraitInfo, IPlaceBuildingDecorationInfo
	{
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			yield return new RangeCircleRenderable(
				centerPosition,
				ai.TraitInfo<CreatesShroudInfo>().Range,
				0,
				Color.FromArgb(128, Color.Cyan),
				Color.FromArgb(96, Color.Black));

			foreach (var a in w.ActorsWithTrait<RenderShroudCircle>())
				if (a.Actor.Owner.IsAlliedWith(w.RenderPlayer))
					foreach (var r in a.Trait.RenderAfterWorld(wr))
						yield return r;
		}

		public object Create(ActorInitializer init) { return new RenderShroudCircle(init.Self); }
	}

	class RenderShroudCircle : IPostRenderSelection
	{
		Actor self;

		public RenderShroudCircle(Actor self) { this.self = self; }

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				self.Info.TraitInfo<CreatesShroudInfo>().Range,
				0,
				Color.FromArgb(128, Color.Cyan),
				Color.FromArgb(96, Color.Black));
		}
	}
}