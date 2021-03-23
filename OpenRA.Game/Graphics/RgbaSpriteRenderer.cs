#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Graphics
{
	public class RgbaSpriteRenderer
	{
		readonly SpriteRenderer parent;

		public RgbaSpriteRenderer(SpriteRenderer parent)
		{
			this.parent = parent;
		}

		public void DrawSprite(Sprite s, in float3 location, in float3 size)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, location, 0, size);
		}

		public void DrawSprite(Sprite s, in float3 location)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, location, 0, s.Size);
		}

		public void DrawSprite(Sprite s, in float3 a, in float3 b, in float3 c, in float3 d)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, a, b, c, d);
		}

		public void DrawSprite(Sprite s, in float3 location, in float3 size, in float3 tint, float alpha)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, location, 0, size, tint, alpha);
		}

		public void DrawSprite(Sprite s, in float3 a, in float3 b, in float3 c, in float3 d, in float3 tint, float alpha)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, a, b, c, d, tint, alpha);
		}
	}
}
