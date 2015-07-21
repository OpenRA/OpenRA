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
	[Desc("Draw a circle indicating my upgrade of nearby range.")]
	class RenderUpgradeRangeCircleInfo : ITraitInfo, IPlaceBuildingDecoration, Requires<UpgradeActorsNearInfo>
	{
		public readonly string RangeCircleType = null;

		[Desc("Color to use")]
		public readonly Color Color = Color.BlueViolet;

		IEnumerable<UpgradeActorsNearInfo> infos = null;

		public IEnumerable<UpgradeActorsNearInfo> GetInfos(ActorInfo ai)
		{
			if (infos == null)
				infos = ai.Traits.WithInterface<UpgradeActorsNearInfo>()
					.Where(t => t.RangeCircleType == RangeCircleType);
			return infos;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			var infos = GetInfos(ai);
			var range = infos.Any() ? infos.Select(a => a.Range).Max() : WDist.Zero;

			if (range == WDist.Zero)
				yield break;

			yield return new RangeCircleRenderable(
				centerPosition,
				range,
				0,
				Color.FromArgb(128, Color),
				Color.FromArgb(96, Color.Black));

			foreach (var a in w.ActorsWithTrait<RenderUpgradeRangeCircle>())
				if (a.Actor.Owner.IsAlliedWith(w.RenderPlayer))
					if (a.Actor.Info.Traits.Get<RenderUpgradeRangeCircleInfo>().RangeCircleType == RangeCircleType)
						foreach (var r in a.Trait.RenderAfterWorld(wr))
							yield return r;
		}

		public object Create(ActorInitializer init) { return new RenderUpgradeRangeCircle(init.Self, this); }
	}

	class RenderUpgradeRangeCircle : IPostRenderSelection
	{
		readonly Actor self;
		readonly WDist range;
		readonly Color color;

		public RenderUpgradeRangeCircle(Actor self, RenderUpgradeRangeCircleInfo info)
		{
			this.self = self;
			var infos = info.GetInfos(self.Info);
			range = infos.Any() ? infos.Select(t => t.Range).Max() : WDist.Zero;
			color = info.Color;
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			if (range == WDist.Zero)
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				range,
				0,
				Color.FromArgb(128, color),
				Color.FromArgb(96, Color.Black));
		}
	}
}
