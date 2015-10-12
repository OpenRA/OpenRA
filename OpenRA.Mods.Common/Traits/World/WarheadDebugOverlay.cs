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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Part of the combat overlay from DeveloperMode. Attach this to the world actor.")]
	public class WarheadDebugOverlayInfo : ITraitInfo
	{
		public readonly int DisplayDuration = 25;

		public object Create(ActorInitializer init) { return new WarheadDebugOverlay(this); }
	}

	public class WarheadDebugOverlay : IPostRender
	{
		class WHImpact
		{
			public readonly WPos CenterPosition;
			public readonly WDist[] Range;
			public int Time;

			public WDist OuterRange
			{
				get { return Range[Range.Length - 1]; }
			}

			public WHImpact(WPos pos, WDist[] range, int time)
			{
				CenterPosition = pos;
				Range = range;
				Time = time;
			}
		}

		readonly WarheadDebugOverlayInfo info;
		readonly List<WHImpact> impacts = new List<WHImpact>();

		public WarheadDebugOverlay(WarheadDebugOverlayInfo info)
		{
			this.info = info;
		}

		public void AddImpact(WPos pos, WDist[] range)
		{
			impacts.Add(new WHImpact(pos, range, info.DisplayDuration));
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			foreach (var i in impacts)
			{
				var alpha = 255.0f * i.Time / info.DisplayDuration;
				var rangeStep = alpha / i.Range.Length;

				wr.DrawRangeCircle(i.CenterPosition, i.OuterRange, Color.FromArgb((int)alpha, Color.Red));

				foreach (var r in i.Range)
				{
					var tl = wr.ScreenPosition(i.CenterPosition - new WVec(r.Length, r.Length, 0));
					var br = wr.ScreenPosition(i.CenterPosition + new WVec(r.Length, r.Length, 0));
					var rect = RectangleF.FromLTRB(tl.X, tl.Y, br.X, br.Y);

					Game.Renderer.WorldLineRenderer.FillEllipse(rect, Color.FromArgb((int)alpha, Color.Red));

					alpha -= rangeStep;
				}

				if (!wr.World.Paused)
					i.Time--;
			}

			impacts.RemoveAll(i => i.Time == 0);
		}
	}
}
