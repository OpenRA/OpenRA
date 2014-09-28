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
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Renders an arbitrary circle when selected or placing a structure")]
	class WithRangeCircleInfo : ITraitInfo, IPlaceBuildingDecoration
	{
		[Desc("Type of range circle. used to decide which circles to draw on other structures during building placement.")]
		public readonly string Type = null;

		[Desc("Color of the circle")]
		public readonly Color Color = Color.FromArgb(128, Color.White);

		[Desc("Range of the circle")]
		public readonly WRange Range = WRange.Zero;

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			yield return new RangeCircleRenderable(
				centerPosition,
				Range,
				0,
				Color,
				Color.FromArgb(96, Color.Black)
			);

			foreach (var a in w.ActorsWithTrait<WithRangeCircle>())
				if (a.Actor.Owner == a.Actor.World.LocalPlayer && a.Trait.Info.Type == Type)
					foreach (var r in a.Trait.RenderAfterWorld(wr))
						yield return r;
		}

		public object Create(ActorInitializer init) { return new WithRangeCircle(init.self, this); }
	}

	class WithRangeCircle : IPostRenderSelection
	{
		public readonly WithRangeCircleInfo Info;
		readonly Actor self;

		public WithRangeCircle(Actor self, WithRangeCircleInfo info)
		{
			this.self = self;
			Info = info;
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer)
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				Info.Range,
				0,
				Info.Color,
				Color.FromArgb(96, Color.Black)
			);
		}
	}
}

