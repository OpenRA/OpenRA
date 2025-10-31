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
}
