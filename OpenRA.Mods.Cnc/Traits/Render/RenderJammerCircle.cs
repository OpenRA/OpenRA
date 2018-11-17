#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Cnc.Traits
{
	// TODO: remove all the Render*Circle duplication
	class RenderJammerCircleInfo : TraitInfo<RenderJammerCircle>, IPlaceBuildingDecorationInfo
	{
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			var jamsMissiles = ai.TraitInfoOrDefault<JamsMissilesInfo>();
			if (jamsMissiles != null)
			{
				yield return new RangeCircleRenderable(
					centerPosition,
					jamsMissiles.Range,
					0,
					Color.FromArgb(128, Color.Red),
					Color.FromArgb(96, Color.Black));
			}

			foreach (var a in w.ActorsWithTrait<RenderJammerCircle>())
				if (a.Actor.Owner.IsAlliedWith(w.RenderPlayer))
					foreach (var r in a.Trait.RenderAboveShroud(a.Actor, wr))
						yield return r;
		}
	}

	class RenderJammerCircle : IRenderAboveShroudWhenSelected
	{
		public IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var jamsMissiles = self.Info.TraitInfoOrDefault<JamsMissilesInfo>();
			if (jamsMissiles != null)
			{
				yield return new RangeCircleRenderable(
					self.CenterPosition,
					jamsMissiles.Range,
					0,
					Color.FromArgb(128, Color.Red),
					Color.FromArgb(96, Color.Black));
			}
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return false; } }
	}
}