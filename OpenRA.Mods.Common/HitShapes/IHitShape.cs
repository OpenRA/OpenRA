#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.HitShapes
{
	public interface IHitShape
	{
		WDist OuterRadius { get; }

		WDist DistanceFromEdge(in WVec v);
		WDist DistanceFromEdge(WPos pos, WPos origin, WRot orientation);

		void Initialize();
		IEnumerable<IRenderable> RenderDebugOverlay(HitShape hs, WorldRenderer wr, WPos origin, WRot orientation);
	}
}
