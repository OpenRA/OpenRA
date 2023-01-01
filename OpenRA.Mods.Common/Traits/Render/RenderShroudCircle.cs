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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class RenderShroudCircleInfo : ConditionalTraitInfo, IPlaceBuildingDecorationInfo
	{
		[Desc("Color of the circle.")]
		public readonly Color Color = Color.FromArgb(128, Color.Cyan);

		[Desc("Range circle line width.")]
		public readonly float Width = 1;

		[Desc("Border color of the circle.")]
		public readonly Color BorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float BorderWidth = 3;

		public IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (!EnabledByDefault)
				return Enumerable.Empty<IRenderable>();

			var localRange = ai.TraitInfos<CreatesShroudInfo>()
				.Where(csi => csi.EnabledByDefault)
				.Select(csi => csi.Range)
				.DefaultIfEmpty(WDist.Zero)
				.Max();

			var localRangeRenderable = new RangeCircleAnnotationRenderable(
				centerPosition,
				localRange,
				0,
				Color,
				Width,
				BorderColor,
				BorderWidth);

			var otherRangeRenderables = w.ActorsWithTrait<RenderShroudCircle>()
				.SelectMany(a => a.Trait.RangeCircleRenderables(a.Actor));

			return otherRangeRenderables.Append(localRangeRenderable);
		}

		public override object Create(ActorInitializer init) { return new RenderShroudCircle(this); }
	}

	class RenderShroudCircle : ConditionalTrait<RenderShroudCircleInfo>, INotifyCreated, IRenderAnnotationsWhenSelected
	{
		readonly RenderShroudCircleInfo info;
		WDist range;

		public RenderShroudCircle(RenderShroudCircleInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			range = self.TraitsImplementing<CreatesShroud>()
				.Select(cs => cs.Info.Range)
				.DefaultIfEmpty(WDist.Zero)
				.Max();

			base.Created(self);
		}

		public IEnumerable<IRenderable> RangeCircleRenderables(Actor self)
		{
			if (IsTraitDisabled)
				yield break;

			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			yield return new RangeCircleAnnotationRenderable(
				self.CenterPosition,
				range,
				0,
				info.Color,
				info.Width,
				info.BorderColor,
				info.BorderWidth);
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RangeCircleRenderables(self);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;
	}
}
