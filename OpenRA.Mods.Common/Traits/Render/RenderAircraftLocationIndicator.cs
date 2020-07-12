#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class RenderAircraftLocationIndicatorInfo : TraitInfo
	{
		[Desc("Armament names")]
		public readonly int Width = 1;

		public readonly Color Color = Color.White;

		public override object Create(ActorInitializer init) { return new RenderAircraftLocationIndicator(init.Self, this); }
	}

	public class RenderAircraftLocationIndicator : IRenderAnnotationsWhenSelected
	{
		readonly RenderAircraftLocationIndicatorInfo info;

		public RenderAircraftLocationIndicator(Actor self, RenderAircraftLocationIndicatorInfo info)
		{
			this.info = info;
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			var altitude = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
			if (altitude.Length == 0)
				yield break;

			var pos = new WPos(self.CenterPosition.X, self.CenterPosition.Y, self.CenterPosition.Z - altitude.Length);

			yield return new CircleAnnotationRenderable(pos, new WDist(64), 1, Color.FromArgb(180, info.Color), true);
			yield return new CircleAnnotationRenderable(pos, new WDist(256), 1, Color.FromArgb(180, info.Color));

			// yield return new AircraftLocationIndicatorRenderable(self.CenterPosition, pos, 1, Color.FromArgb(10, info.Color), Color.FromArgb(180, info.Color));
			yield return new AircraftLocationIndicatorRenderable(self.CenterPosition, pos, info.Width, Color.FromArgb(altitude.Length / 32, 0, 0), Color.FromArgb(altitude.Length / 32, 0, 0));
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable { get { return false; } }
	}
}
