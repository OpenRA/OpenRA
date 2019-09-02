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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public enum RangeCircleVisibility { Always, WhenSelected }

	[Desc("Renders an arbitrary circle when selected or placing a structure")]
	class WithRangeCircleInfo : ConditionalTraitInfo, IPlaceBuildingDecorationInfo
	{
		[Desc("Type of range circle. used to decide which circles to draw on other structures during building placement.")]
		public readonly string Type = null;

		[Desc("Color of the circle")]
		public readonly Color Color = Color.FromArgb(128, Color.White);

		[Desc("If set, the color of the owning player will be used instead of `Color`.")]
		public readonly bool UsePlayerColor = false;

		[Desc("Stances of players which will be able to see the circle.",
			"Valid values are combinations of `None`, `Ally`, `Enemy` and `Neutral`.")]
		public readonly Stance ValidStances = Stance.Ally;

		[Desc("When to show the range circle. Valid values are `Always`, and `WhenSelected`")]
		public readonly RangeCircleVisibility Visible = RangeCircleVisibility.WhenSelected;

		[Desc("Range of the circle")]
		public readonly WDist Range = WDist.Zero;

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (EnabledByDefault)
			{
				yield return new RangeCircleRenderable(
					centerPosition,
					Range,
					0,
					Color,
					Color.FromArgb(96, Color.Black));

				foreach (var a in w.ActorsWithTrait<WithRangeCircle>())
					if (a.Trait.Info.Type == Type)
						foreach (var r in a.Trait.RenderRangeCircle(a.Actor, wr, RangeCircleVisibility.WhenSelected))
							yield return r;
			}
		}

		public override object Create(ActorInitializer init) { return new WithRangeCircle(init.Self, this); }
	}

	class WithRangeCircle : ConditionalTrait<WithRangeCircleInfo>, IRenderAboveShroudWhenSelected, IRenderAboveShroud
	{
		readonly Actor self;

		public WithRangeCircle(Actor self, WithRangeCircleInfo info)
			: base(info)
		{
			this.self = self;
		}

		bool Visible
		{
			get
			{
				if (IsTraitDisabled)
					return false;

				var p = self.World.RenderPlayer;
				return p == null || Info.ValidStances.HasStance(self.Owner.Stances[p]) || (p.Spectating && !p.NonCombatant);
			}
		}

		public IEnumerable<IRenderable> RenderRangeCircle(Actor self, WorldRenderer wr, RangeCircleVisibility visibility)
		{
			if (Info.Visible == visibility && Visible)
				yield return new RangeCircleRenderable(
					self.CenterPosition,
					Info.Range,
					0,
					Info.UsePlayerColor ? self.Owner.Color : Info.Color,
					Color.FromArgb(96, Color.Black));
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return RenderRangeCircle(self, wr, RangeCircleVisibility.WhenSelected);
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return false; } }

		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return RenderRangeCircle(self, wr, RangeCircleVisibility.Always);
		}

		bool IRenderAboveShroud.SpatiallyPartitionable { get { return false; } }
	}
}
