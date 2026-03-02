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
using OpenRA.Primitives;

namespace OpenRA.Effects
{
	public interface IEffect
	{
		void Tick(World world);
		IEnumerable<IRenderable> Render(WorldRenderer r);
	}

	// Identifier interface for effects that are added to ScreenMap
	public interface ISpatiallyPartitionable { }

	public interface IEffectAboveShroud { IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr); }
	public interface IEffectAnnotation { IEnumerable<IRenderable> RenderAnnotation(WorldRenderer wr); }

	// Effect that can provide a tooltip when the cursor is near it.
	public interface IEffectWithTooltip { bool IsNearCursor(WPos cursorWorldPos); string GetTooltip(); }

	// Effect that contributes line segments to the radar/minimap overlay.
	public interface IRadarEffect { IEnumerable<(WPos From, WPos To, Color Color)> RadarLineSegments { get; } }
}
