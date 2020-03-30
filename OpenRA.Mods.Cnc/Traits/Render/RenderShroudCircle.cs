#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class RenderShroudCircleInfo : ITraitInfo, IPlaceBuildingDecorationInfo
	{
		[Desc("Color of the circle.")]
		public readonly Color Color = Color.FromArgb(128, Color.Cyan);

		[Desc("Contrast color of the circle.")]
		public readonly Color ContrastColor = Color.FromArgb(96, Color.Black);

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			var localRange = ai.TraitInfos<CreatesShroudInfo>()
				.Where(csi => csi.EnabledByDefault)
				.Select(csi => csi.Range)
				.DefaultIfEmpty(WDist.Zero)
				.Max();

			var localRangeRenderable = new RangeCircleRenderable(
				centerPosition,
				localRange,
				0,
				Color,
				ContrastColor);

			var otherRangeRenderables = w.ActorsWithTrait<RenderShroudCircle>()
				.SelectMany(a => a.Trait.RangeCircleRenderables(a.Actor, wr));

			return otherRangeRenderables.Append(localRangeRenderable);
		}

		public object Create(ActorInitializer init) { return new RenderShroudCircle(init.Self, this); }
	}

	class RenderShroudCircle : INotifyCreated, IRenderAboveShroudWhenSelected
	{
		readonly RenderShroudCircleInfo info;
		WDist range;

		public RenderShroudCircle(Actor self, RenderShroudCircleInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			range = self.TraitsImplementing<CreatesShroud>()
				.Select(cs => cs.Info.Range)
				.DefaultIfEmpty(WDist.Zero)
				.Max();
		}

		public IEnumerable<IRenderable> RangeCircleRenderables(Actor self, WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				range,
				0,
				info.Color,
				info.ContrastColor);
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return RangeCircleRenderables(self, wr);
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return false; } }
	}
}
