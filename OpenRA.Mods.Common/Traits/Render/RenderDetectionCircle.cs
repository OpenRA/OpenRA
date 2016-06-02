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
	class RenderDetectionCircleInfo : ITraitInfo, Requires<DetectCloakedInfo>
	{
		[Desc("WAngle the Radar update line advances per tick.")]
		public readonly WAngle UpdateLineTick = new WAngle(-1);

		[Desc("Number of trailing Radar update lines.")]
		public readonly int TrailCount = 0;

		[Desc("Color of the circle and scanner update line.")]
		public readonly Color Color = Color.FromArgb(128, Color.LimeGreen);

		[Desc("Contrast color of the circle and scanner update line.")]
		public readonly Color ContrastColor = Color.FromArgb(96, Color.Black);

		public object Create(ActorInitializer init) { return new RenderDetectionCircle(init.Self, this); }
	}

	class RenderDetectionCircle : ITick, IPostRenderSelection
	{
		readonly RenderDetectionCircleInfo info;
		readonly Actor self;
		WAngle lineAngle;

		public RenderDetectionCircle(Actor self, RenderDetectionCircleInfo info)
		{
			this.info = info;
			this.self = self;
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var range = self.TraitsImplementing<DetectCloaked>()
				.Where(a => !a.IsTraitDisabled)
				.Select(a => a.Info.Range)
				.Append(WDist.Zero).Max();

			if (range == WDist.Zero)
				yield break;

			yield return new DetectionCircleRenderable(
				self.CenterPosition,
				range,
				0,
				info.TrailCount,
				info.UpdateLineTick,
				lineAngle,
				info.Color,
				info.ContrastColor);
		}

		public void Tick(Actor self)
		{
			lineAngle += info.UpdateLineTick;
		}
	}
}
