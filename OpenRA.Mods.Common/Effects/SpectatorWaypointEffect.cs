#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Effects
{
	// Local-only effect that renders a waypoint path for spectators.
	public class SpectatorWaypointEffect : IEffect, IEffectAnnotation, IEffectWithTooltip, IRadarEffect
	{
		const int LineWidth = 2;
		const int MarkerSize = 3;

		// Tolerance squared in world units (~half a cell radius) for cursor proximity detection.
		const long HoverToleranceSq = 512L * 512;

		readonly IReadOnlyList<WPos> waypoints;
		readonly int duration;
		readonly Color color;
		readonly string spectatorName;
		int tick;

		public SpectatorWaypointEffect(IReadOnlyList<WPos> waypoints, int duration, Color color, string spectatorName)
		{
			this.waypoints = waypoints;
			this.duration = duration;
			this.color = color;
			this.spectatorName = spectatorName;
		}

		void IEffect.Tick(World world)
		{
			if (duration > 0 && tick++ >= duration)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr)
		{
			return SpriteRenderable.None;
		}

		IEnumerable<IRenderable> IEffectAnnotation.RenderAnnotation(WorldRenderer wr)
		{
			if (waypoints.Count < 2)
				yield break;

			yield return new TargetLineRenderable(waypoints, color, LineWidth, MarkerSize);
		}

		bool IEffectWithTooltip.IsNearCursor(WPos cursorWorldPos)
		{
			for (var i = 0; i < waypoints.Count - 1; i++)
				if (SquaredDistanceToSegment(cursorWorldPos, waypoints[i], waypoints[i + 1]) <= HoverToleranceSq)
					return true;

			return false;
		}

		string IEffectWithTooltip.GetTooltip() => spectatorName;

		IEnumerable<(WPos From, WPos To, Color Color)> IRadarEffect.RadarLineSegments
		{
			get
			{
				for (var i = 0; i < waypoints.Count - 1; i++)
					yield return (waypoints[i], waypoints[i + 1], color);
			}
		}

		static long SquaredDistanceToSegment(WPos point, WPos segA, WPos segB)
		{
			var ab = segB - segA;
			var ap = point - segA;
			var abLenSq = (long)ab.X * ab.X + (long)ab.Y * ab.Y;

			if (abLenSq == 0)
			{
				// Degenerate segment: both endpoints are the same.
				var dx = (long)point.X - segA.X;
				var dy = (long)point.Y - segA.Y;
				return dx * dx + dy * dy;
			}

			var t = ((long)ap.X * ab.X + (long)ap.Y * ab.Y) * 1024 / abLenSq;
			t = System.Math.Clamp(t, 0, 1024);

			var ex = (long)point.X - segA.X - ab.X * t / 1024;
			var ey = (long)point.Y - segA.Y - ab.Y * t / 1024;
			return ex * ex + ey * ey;
		}
	}
}
