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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Part of the combat overlay from `" + nameof(DeveloperMode) + "`. Attach this to the world actor.")]
	public class WarheadDebugOverlayInfo : TraitInfo
	{
		public readonly int DisplayDuration = 25;

		public override object Create(ActorInitializer init) { return new WarheadDebugOverlay(this); }
	}

	public class WarheadDebugOverlay : IRenderAnnotations
	{
		class WHImpact
		{
			public readonly WPos CenterPosition;
			public readonly WDist[] Range;
			public readonly Color Color;
			public int Time;

			public WDist OuterRange => Range[Range.Length - 1];

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

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			foreach (var i in impacts)
			{
				var alpha = 255.0f * i.Time / info.DisplayDuration;
				var rangeStep = alpha / i.Range.Length;

				yield return new CircleAnnotationRenderable(i.CenterPosition, i.OuterRange, 1, Color.FromArgb((int)alpha, i.Color));

				foreach (var r in i.Range)
				{
					yield return new CircleAnnotationRenderable(i.CenterPosition, r, 1, Color.FromArgb((int)alpha, i.Color), true);
					alpha -= rangeStep;
				}

				if (!wr.World.Paused)
					i.Time--;
			}

			impacts.RemoveAll(i => i.Time == 0);
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
