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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public enum RangeCircleMode { Maximum, Minimum }

	[Desc("Draw a circle indicating my weapon's range.")]
	class RenderRangeCircleInfo : ITraitInfo, IPlaceBuildingDecorationInfo, IRulesetLoaded, Requires<AttackBaseInfo>
	{
		public readonly string RangeCircleType = null;

		[Desc("Range to draw if no armaments are available.")]
		public readonly WDist FallbackRange = WDist.Zero;

		[Desc("Which circle to show. Valid values are `Maximum`, and `Minimum`.")]
		public readonly RangeCircleMode RangeCircleMode = RangeCircleMode.Maximum;

		[Desc("Color of the circle.")]
		public readonly Color Color = Color.FromArgb(128, Color.Yellow);

		[Desc("Color of the border of the circle.")]
		public readonly Color BorderColor = Color.FromArgb(96, Color.Black);

		// Computed range
		Lazy<WDist> range;

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (range == null || range.Value == WDist.Zero)
				return SpriteRenderable.None;

			var localRange = new RangeCircleRenderable(
				centerPosition,
				range.Value,
				0,
				this.Color,
				this.BorderColor);

			var otherRanges = w.ActorsWithTrait<RenderRangeCircle>()
				.Where(a => a.Trait.Info.RangeCircleType == RangeCircleType)
				.SelectMany(a => a.Trait.RangeCircleRenderables(wr));

			return otherRanges.Append(localRange);
		}

		public object Create(ActorInitializer init) { return new RenderRangeCircle(init.Self, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			// ArmamentInfo.ModifiedRange is set by RulesetLoaded, and may not have been initialized yet.
			// Defer this lookup until we really need it to ensure we get the correct value.
			range = Exts.Lazy(() =>
			{
				var armaments = ai.TraitInfos<ArmamentInfo>().Where(a => a.EnabledByDefault);
				if (!armaments.Any())
					return FallbackRange;

				return armaments.Max(a => a.ModifiedRange);
			});
		}
	}

	class RenderRangeCircle : IRenderAboveShroudWhenSelected
	{
		public readonly RenderRangeCircleInfo Info;
		readonly Actor self;
		readonly AttackBase attack;

		public RenderRangeCircle(Actor self, RenderRangeCircleInfo info)
		{
			Info = info;

			this.self = self;
			attack = self.Trait<AttackBase>();
		}

		public IEnumerable<IRenderable> RangeCircleRenderables(WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var range = Info.RangeCircleMode == RangeCircleMode.Minimum ? attack.GetMinimumRange() : attack.GetMaximumRange();
			if (range == WDist.Zero)
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				range,
				0,
				Info.Color,
				Info.BorderColor);
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return RangeCircleRenderables(wr);
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return false; } }
	}
}
