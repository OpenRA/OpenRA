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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Draw a circle indicating my weapon's range.")]
	class RenderRangeCircleInfo : ITraitInfo, IPlaceBuildingDecorationInfo, IRulesetLoaded, Requires<AttackBaseInfo>
	{
		public readonly string RangeCircleType = null;

		[Desc("Range to draw if no armaments are available")]
		public readonly WDist FallbackRange = WDist.Zero;

		// Computed range
		WDist range;

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (range == WDist.Zero)
				yield break;

			yield return new RangeCircleRenderable(
				centerPosition,
				range,
				0,
				Color.FromArgb(128, Color.Yellow),
				Color.FromArgb(96, Color.Black));

			foreach (var a in w.ActorsWithTrait<RenderRangeCircle>())
				if (a.Actor.Owner.IsAlliedWith(w.RenderPlayer))
					if (a.Actor.Info.TraitInfo<RenderRangeCircleInfo>().RangeCircleType == RangeCircleType)
						foreach (var r in a.Trait.RenderAfterWorld(wr))
							yield return r;
		}

		public object Create(ActorInitializer init) { return new RenderRangeCircle(init.Self); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var armaments = ai.TraitInfos<ArmamentInfo>().Where(a => a.UpgradeMinEnabledLevel == 0);

			if (armaments.Any())
				range = armaments.Select(a => a.ModifiedRange).Max();
			else
				range = FallbackRange;
		}
	}

	class RenderRangeCircle : IPostRenderSelection
	{
		readonly Actor self;
		readonly AttackBase attack;

		public RenderRangeCircle(Actor self)
		{
			this.self = self;
			attack = self.Trait<AttackBase>();
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var range = attack.GetMaximumRange();
			if (range == WDist.Zero)
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				range,
				0,
				Color.FromArgb(128, Color.Yellow),
				Color.FromArgb(96, Color.Black));
		}
	}
}
