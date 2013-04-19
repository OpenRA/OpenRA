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

namespace OpenRA.Mods.RA
{
	//todo: remove all the Render*Circle duplication
	class RenderJammerCircleInfo : TraitInfo<RenderJammerCircle>, IPlaceBuildingDecoration
	{
		public void Render(WorldRenderer wr, World w, ActorInfo ai, PPos centerLocation)
		{
			var jamsMissiles = ai.Traits.GetOrDefault<JamsMissilesInfo>();
			if (jamsMissiles != null)
				RenderJammerCircle.DrawRangeCircle(wr, centerLocation.ToFloat2(), jamsMissiles.Range, Color.Red);

			var jamsRadar = ai.Traits.GetOrDefault<JamsRadarInfo>();
			if (jamsRadar != null)
				RenderJammerCircle.DrawRangeCircle(wr, centerLocation.ToFloat2(), jamsRadar.Range, Color.Blue);

			foreach (var a in w.ActorsWithTrait<RenderJammerCircle>())
				if (a.Actor.Owner == a.Actor.World.LocalPlayer)
					a.Trait.RenderBeforeWorld(wr, a.Actor);
		}
	}

	public class RenderJammerCircle : IPreRenderSelection
	{
		public void RenderBeforeWorld(WorldRenderer wr, Actor self)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			var jamsMissiles = self.Info.Traits.GetOrDefault<JamsMissilesInfo>();
			if (jamsMissiles != null)
				DrawRangeCircle(wr, self.CenterLocation.ToFloat2(), jamsMissiles.Range, Color.Red);

			var jamsRadar = self.Info.Traits.GetOrDefault<JamsRadarInfo>();
			if (jamsRadar != null)
				DrawRangeCircle(wr, self.CenterLocation.ToFloat2(), jamsRadar.Range, Color.Blue);
		}

		public static void DrawRangeCircle(WorldRenderer wr, float2 location, int range, Color color)
		{
			wr.DrawRangeCircleWithContrast(
				Color.FromArgb(128, color),
				location,
				range,
				Color.FromArgb(96, Color.Black),
				1);
		}
	}
}

