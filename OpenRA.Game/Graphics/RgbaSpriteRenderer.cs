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
using System.Numerics;

namespace OpenRA.Graphics
{
	public class RgbaSpriteRenderer
	{
		readonly SpriteRenderer parent;

		public RgbaSpriteRenderer(SpriteRenderer parent)
		{
			this.parent = parent;
		}

		public void DrawSprite(Sprite s, in Vector3 location, in Vector3 scale, float rotation = 0f)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, 0, location, scale, rotation);
		}

		public void DrawSprite(Sprite s, in Vector3 location, float scale = 1f, float rotation = 0f)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, 0, location, scale, rotation);
		}

		public void DrawSprite(Sprite s, in Vector3 location, float scale, in Vector3 tint, float alpha, float rotation = 0f)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, 0, location, scale, tint, alpha, rotation);
		}

		public void DrawSprite(Sprite s, in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 d, in Vector3 tint, float alpha)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, 0, a, b, c, d, tint, alpha);
		}
	}
}
