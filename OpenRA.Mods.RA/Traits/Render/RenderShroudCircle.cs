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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class RenderShroudCircleInfo : TraitInfo<RenderShroudCircle>, IPlaceBuildingDecorationInfo
	{
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			var localRange = new RangeCircleRenderable(
				centerPosition,
				ai.TraitInfo<CreatesShroudInfo>().Range,
				0,
				Color.FromArgb(128, Color.Cyan),
				Color.FromArgb(96, Color.Black));

			var otherRanges = w.ActorsWithTrait<RenderShroudCircle>()
				.SelectMany(a => a.Trait.RangeCircleRenderables(a.Actor, wr));

			return otherRanges.Append(localRange);
		}
	}

	class RenderShroudCircle : IRenderAboveShroudWhenSelected
	{
		public IEnumerable<IRenderable> RangeCircleRenderables(Actor self, WorldRenderer wr)
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

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return RangeCircleRenderables(self, wr);
		}
	}
}