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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	class RenderDetectionCircleInfo : TraitInfo, Requires<DetectCloakedInfo>
	{
		[Desc("WAngle the Radar update line advances per tick.")]
		public readonly WAngle UpdateLineTick = new WAngle(-1);

		[Desc("Number of trailing Radar update lines.")]
		public readonly int TrailCount = 0;

		[Desc("Color of the circle and scanner update line.")]
		public readonly Color Color = Color.FromArgb(128, Color.LimeGreen);

		[Desc("Contrast color of the circle and scanner update line.")]
		public readonly Color ContrastColor = Color.FromArgb(96, Color.Black);

		public override object Create(ActorInitializer init) { return new RenderDetectionCircle(init.Self, this); }
	}

	class RenderDetectionCircle : ITick, IRenderAnnotationsWhenSelected
	{
		readonly RenderDetectionCircleInfo info;
		WAngle lineAngle;

		public RenderDetectionCircle(Actor self, RenderDetectionCircleInfo info)
		{
			this.info = info;
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var range = self.TraitsImplementing<DetectCloaked>()
				.Select(a => a.Range)
				.Append(WDist.Zero).Max();

			if (range == WDist.Zero)
				yield break;

			yield return new DetectionCircleAnnotationRenderable(
				self.CenterPosition,
				range,
				0,
				info.TrailCount,
				info.UpdateLineTick,
				lineAngle,
				info.Color,
				info.ContrastColor);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable { get { return false; } }

		void ITick.Tick(Actor self)
		{
			lineAngle += info.UpdateLineTick;
		}
	}
}
