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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Part of the combat overlay from DeveloperMode. Attach this to the world actor.")]
	public class WarheadDebugOverlayInfo : ITraitInfo
	{
		public readonly int DisplayDuration = 25;

		public object Create(ActorInitializer init) { return new WarheadDebugOverlay(this); }
	}

	public class WarheadDebugOverlay : IRenderAboveWorld
	{
		class WHImpact
		{
			public readonly WPos CenterPosition;
			public readonly WDist[] Range;
			public readonly Color Color;
			public int Time;

			public WDist OuterRange
			{
				get { return Range[Range.Length - 1]; }
			}

			public WHImpact(WPos pos, WDist[] range, int time, Color color)
			{
				CenterPosition = pos;
				Range = range;
				Color = color;
				Time = time;
			}
		}

		readonly WarheadDebugOverlayInfo info;
		readonly List<WHImpact> impacts = new List<WHImpact>();

		public WarheadDebugOverlay(WarheadDebugOverlayInfo info)
		{
			this.info = info;
		}

		public void AddImpact(WPos pos, WDist[] range, Color color)
		{
			impacts.Add(new WHImpact(pos, range, info.DisplayDuration, color));
		}

		void IRenderAboveWorld.RenderAboveWorld(Actor self, WorldRenderer wr)
		{
			foreach (var i in impacts)
			{
				var alpha = 255.0f * i.Time / info.DisplayDuration;
				var rangeStep = alpha / i.Range.Length;

				RangeCircleRenderable.DrawRangeCircle(wr, i.CenterPosition, i.OuterRange,
					1, Color.FromArgb((int)alpha, i.Color), 0, i.Color);

				foreach (var r in i.Range)
				{
					var tl = wr.Screen3DPosition(i.CenterPosition - new WVec(r.Length, r.Length, 0));
					var br = wr.Screen3DPosition(i.CenterPosition + new WVec(r.Length, r.Length, 0));

					Game.Renderer.WorldRgbaColorRenderer.FillEllipse(tl, br, Color.FromArgb((int)alpha, i.Color));

					alpha -= rangeStep;
				}

				if (!wr.World.Paused)
					i.Time--;
			}

			impacts.RemoveAll(i => i.Time == 0);
		}
	}
}
