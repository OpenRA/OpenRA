#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	//todo: remove all the Render*Circle duplication
	class RenderJammerCircleInfo : ITraitInfo, IPlaceBuildingDecoration
	{
		public void Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			var jamsMissiles = ai.Traits.GetOrDefault<JamsMissilesInfo>();
			if (jamsMissiles != null)
				RenderJammerCircle.DrawRangeCircle(wr, centerPosition, jamsMissiles.Range, Color.Red);

			var jamsRadar = ai.Traits.GetOrDefault<JamsRadarInfo>();
			if (jamsRadar != null)
				RenderJammerCircle.DrawRangeCircle(wr, centerPosition, jamsRadar.Range, Color.Blue);

			foreach (var a in w.ActorsWithTrait<RenderJammerCircle>())
				if (a.Actor.Owner == a.Actor.World.LocalPlayer)
					a.Trait.RenderAfterWorld(wr);
		}

		public object Create(ActorInitializer init) { return new RenderJammerCircle(init.self); }
	}

	class RenderJammerCircle : IPostRenderSelection
	{
		Actor self;

		public RenderJammerCircle(Actor self) { this.self = self; }

		public void RenderAfterWorld(WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			var jamsMissiles = self.Info.Traits.GetOrDefault<JamsMissilesInfo>();
			if (jamsMissiles != null)
				DrawRangeCircle(wr, self.CenterPosition, jamsMissiles.Range, Color.Red);

			var jamsRadar = self.Info.Traits.GetOrDefault<JamsRadarInfo>();
			if (jamsRadar != null)
				DrawRangeCircle(wr, self.CenterPosition, jamsRadar.Range, Color.Blue);
		}

		public static void DrawRangeCircle(WorldRenderer wr, WPos pos, int range, Color color)
		{
			wr.DrawRangeCircleWithContrast(
				pos,
				WRange.FromCells(range),
				Color.FromArgb(128, color),
				Color.FromArgb(96, Color.Black)
			);
		}
	}
}

