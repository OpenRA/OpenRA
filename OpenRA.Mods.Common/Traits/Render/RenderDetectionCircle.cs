#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class RenderDetectionCircleInfo : ITraitInfo, Requires<DetectCloakedInfo>
	{
		[Desc("Draw a rotating radar scanner update line, disabled by default.")]
		public readonly bool DrawUpdateLine = false;

		[Desc("WAngle the Radar update line advances per tick.")]
		public readonly WAngle UpdateLineTick = new WAngle(-1);

		[Desc("Number of trailing Radar update lines, will only draw one line if zero.")]
		public readonly int LineTrailLength = 3;

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

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				range,
				0,
				info.Color,
				info.ContrastColor);

			if (info.DrawUpdateLine)
			{
				for (var i = info.LineTrailLength; i >= 0; i--)
				{
					var angle = lineAngle - new WAngle(i * (info.UpdateLineTick.Angle <= 512 ? 1 : -1));
					var length = range.Length * new WVec(angle.Cos(), angle.Sin(), 0) / 1024;
					var alpha = info.Color.A - (info.LineTrailLength > 0 ? i * info.Color.A / info.LineTrailLength : 0);
					yield return new BeamRenderable(
						self.CenterPosition,
						0,
						length,
						3,
						Color.FromArgb(alpha, info.ContrastColor));
					yield return new BeamRenderable(
						self.CenterPosition,
						0,
						length,
						1,
						Color.FromArgb(alpha, info.Color));
				}
			}
		}

		public void Tick(Actor self)
		{
			lineAngle += info.UpdateLineTick;
		}
	}
}
