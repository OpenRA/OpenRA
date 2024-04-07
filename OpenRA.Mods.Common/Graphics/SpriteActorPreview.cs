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

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public class SpriteActorPreview : IActorPreview
	{
		public readonly Animation Animation;
		readonly Func<WVec> offset;
		readonly Func<int> zOffset;
		readonly PaletteReference pr;

		public SpriteActorPreview(Animation animation, Func<WVec> offset, Func<int> zOffset, PaletteReference pr)
		{
			Animation = animation;
			this.offset = offset;
			this.zOffset = zOffset;
			this.pr = pr;
		}

		void IActorPreview.Tick() { Animation.Tick(); }

		IEnumerable<IRenderable> IActorPreview.RenderUI(WorldRenderer wr, int2 pos, float scale)
		{
			return Animation.RenderUI(wr, pos, offset(), zOffset(), pr, scale);
		}

		IEnumerable<IRenderable> IActorPreview.Render(WorldRenderer wr, WPos pos)
		{
			return Animation.Render(pos, offset(), zOffset(), pr);
		}

		IEnumerable<Rectangle> IActorPreview.ScreenBounds(WorldRenderer wr, WPos pos)
		{
			yield return Animation.ScreenBounds(wr, pos, offset());
		}
	}
}
