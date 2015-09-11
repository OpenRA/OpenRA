#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Flags]
	public enum RangeAggregate { Min = 1, Max, MinMax }

	[Desc("Draw a circle indicating range when selected or placing a structure.")]
	class WithRangeCircleInfo : ITraitInfo, IPlaceBuildingDecoration, IRanged, IProvidesRanges
	{
		[Desc("Used to decide which circles to draw on other actors during building placement.")]
		public readonly string Name = null;

		[Desc("Type of range for circle.", "See range types provided by other traits.", "Examples: attack")]
		public readonly string Type = null;

		[Desc("Variant of range type for circle.", "Used to decide which traits matching type to use.",
			"default is all variants", "Examples: Air - for attacks against air targets, Underwater - for detecting subs")]
		public readonly string Variant = null;

		[Desc("Color of the circle")]
		public readonly Color Color = Color.FromArgb(128, Color.White);

		[Desc("Outline color of the circle")]
		public readonly Color ContrastColor = Color.FromArgb(96, Color.Black);

		[Desc("Which circle(s): Min, Max, MinMax")]
		public readonly RangeAggregate RangesRendered = RangeAggregate.MinMax;

		[Desc("Range to draw if no ranged traits with matching range type (& variant) are available (0 disables).")]
		public readonly WDist Range = WDist.Zero;

		[Desc("Only draw range circle during own placement.")]
		public readonly bool OwnPlacementOnly = false;

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			// maxRange defaults to Range if RangesRendered != Min; otherwise, minRange does
			var maxRange = RangesRendered != RangeAggregate.Min ? Range : WDist.Zero;
			var minRange = RangesRendered == RangeAggregate.Min ? Range : WDist.MaxValue;
			var getMax = RangesRendered.HasFlag(RangeAggregate.Max);
			var getMin = RangesRendered.HasFlag(RangeAggregate.Min);
			foreach (var infos in ai.Traits.WithInterface<IProvidesRangesInfo>())
				if (infos.ProvidesRanges(Type, Variant, ai, w))
					foreach (var r in infos.GetRanges(Type, Variant, ai, w))
					{
						var max = r.GetMaximumRange(ai, w);
						if (getMax && max > maxRange)
							maxRange = max;

						if (getMin && max != WDist.Zero)
						{
							var min = r.GetMinimumRange(ai, w);
							if (min > minRange)
								minRange = min;
						}
					}

			if (maxRange != WDist.Zero)
			{
				if (getMin && minRange != WDist.Zero && minRange != WDist.MaxValue)
					yield return new RangeCircleRenderable(
						centerPosition,
						minRange,
						0,
						Color,
						ContrastColor);

				if (getMax)
					yield return new RangeCircleRenderable(
						centerPosition,
						maxRange,
						0,
						Color,
						ContrastColor);
			}

			foreach (var a in w.ActorsWithTrait<WithRangeCircle>())
				if (a.Actor.Owner.IsAlliedWith(w.RenderPlayer) && a.Trait.Info.Name == Name)
					foreach (var r in a.Trait.RenderAfterWorld(wr))
						yield return r;
		}

		public object Create(ActorInitializer init)
		{
			return OwnPlacementOnly ? new WithOwnPlacementRangeCircle() as object : new WithRangeCircle(init.Self, this);
		}

		public bool ProvidesRanges(string type, string variant) { return false; }
		public IEnumerable<IRanged> GetRanges(string type, string variant) { yield return this; }
		public WDist GetMinimumRange(ActorInfo ai, World w) { return WDist.Zero; }
		public WDist GetMaximumRange(ActorInfo ai, World w) { return Range; }
	}

	class WithOwnPlacementRangeCircle { }

	class WithRangeCircle : IPostRenderSelection, INotifyCreated
	{
		public readonly WithRangeCircleInfo Info;
		readonly Actor self;
		IProvidesRanges[] providedRanges;

		public WithRangeCircle(Actor self, WithRangeCircleInfo info)
		{
			this.self = self;
			Info = info;
		}

		public void Created(Actor self)
		{
			if (string.IsNullOrEmpty(Info.Type))
				providedRanges = new IProvidesRanges[] { Info };
			else
			{
				var providedRangesList = new List<IProvidesRanges>();
				foreach (var ranges in self.TraitsImplementing<IProvidesRanges>())
					if (ranges.ProvidesRanges(Info.Type, Info.Variant))
						providedRangesList.Add(ranges);
				providedRanges = providedRangesList.ToArray();
			}
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			// maxRange defaults to Range if RangesRendered != Min; otherwise, minRange does
			var maxRange = Info.RangesRendered != RangeAggregate.Min ? Info.Range : WDist.Zero;
			var minRange = Info.RangesRendered == RangeAggregate.Min ? Info.Range : WDist.MaxValue;
			var getMax = Info.RangesRendered.HasFlag(RangeAggregate.Max);
			var getMin = Info.RangesRendered.HasFlag(RangeAggregate.Min);
			foreach (var ranges in providedRanges)
				foreach (var r in ranges.GetRanges(Info.Type, Info.Variant))
				{
					var max = r.GetMaximumRange();
					if (getMax)
					{
						if (max > maxRange)
							maxRange = max;
					}

					if (getMin && max != WDist.Zero)
					{
						var min = r.GetMinimumRange();
						if (min > minRange)
							minRange = min;
					}
				}

			if (getMax && maxRange == WDist.Zero)
				yield break;

			if (getMin && minRange != WDist.Zero && minRange != WDist.MaxValue)
				yield return new RangeCircleRenderable(
					self.CenterPosition,
					minRange,
					0,
					Info.Color,
					Info.ContrastColor);

			if (getMax)
				yield return new RangeCircleRenderable(
					self.CenterPosition,
					maxRange,
					0,
					Info.Color,
					Info.ContrastColor);
		}
	}
}