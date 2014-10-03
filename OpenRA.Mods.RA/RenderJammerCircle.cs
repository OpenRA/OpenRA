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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	// TODO: remove all the Render*Circle duplication
	class RenderJammerCircleInfo : ITraitInfo, IPlaceBuildingDecoration
	{
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			var jamsMissiles = ai.Traits.GetOrDefault<JamsMissilesInfo>();
			if (jamsMissiles != null)
			{
				yield return new RangeCircleRenderable(
					centerPosition,
					WRange.FromCells(jamsMissiles.Range),
					0,
					Color.FromArgb(128, Color.Red),
					Color.FromArgb(96, Color.Black));
			}

			var jamsRadar = ai.Traits.GetOrDefault<JamsRadarInfo>();
			if (jamsRadar != null)
			{
				yield return new RangeCircleRenderable(
					centerPosition,
					WRange.FromCells(jamsRadar.Range),
					0,
					Color.FromArgb(128, Color.Blue),
					Color.FromArgb(96, Color.Black));
			}

			foreach (var a in w.ActorsWithTrait<RenderJammerCircle>())
				if (a.Actor.Owner == a.Actor.World.LocalPlayer)
					foreach (var r in a.Trait.RenderAfterWorld(wr))
						yield return r;
		}

		public object Create(ActorInitializer init) { return new RenderJammerCircle(init.self); }
	}

	class RenderJammerCircle : IPostRenderSelection
	{
		Actor self;

		public RenderJammerCircle(Actor self) { this.self = self; }

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer)
				yield break;

			var jamsMissiles = self.Info.Traits.GetOrDefault<JamsMissilesInfo>();
			if (jamsMissiles != null)
			{
				yield return new RangeCircleRenderable(
					self.CenterPosition,
					WRange.FromCells(jamsMissiles.Range),
					0,
					Color.FromArgb(128, Color.Red),
					Color.FromArgb(96, Color.Black));
			}

			var jamsRadar = self.Info.Traits.GetOrDefault<JamsRadarInfo>();
			if (jamsRadar != null)
			{
				yield return new RangeCircleRenderable(
					self.CenterPosition,
					WRange.FromCells(jamsRadar.Range),
					0,
					Color.FromArgb(128, Color.Blue),
					Color.FromArgb(96, Color.Black));
			}
		}
	}
}

