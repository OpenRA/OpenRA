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

using System;
using System.Collections.Generic;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Graphics
{
	public class SpriteActorPreview : IActorPreview
	{
		readonly Animation animation;
		readonly Func<WVec> offset;
		readonly Func<int> zOffset;
		readonly PaletteReference pr;
		readonly float scale;

		public SpriteActorPreview(Animation animation, Func<WVec> offset, Func<int> zOffset, PaletteReference pr, float scale)
		{
			this.animation = animation;
			this.offset = offset;
			this.zOffset = zOffset;
			this.pr = pr;
			this.scale = scale;
		}

		public void Tick() { animation.Tick(); }

		public IEnumerable<IRenderable> Render(WorldRenderer wr, WPos pos)
		{
			return animation.Render(pos, offset(), zOffset(), pr, scale);
		}
	}
}
