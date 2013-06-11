#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;

namespace OpenRA.Graphics
{
	public class Sprite
	{
		public readonly Rectangle bounds;
		public readonly Sheet sheet;
		public readonly TextureChannel channel;
		public readonly float2 size;
		public readonly float2 offset;
		readonly float2[] textureCoords;

		public Sprite(Sheet sheet, Rectangle bounds, TextureChannel channel)
			: this(sheet, bounds, float2.Zero, channel) {}

		public Sprite(Sheet sheet, Rectangle bounds, float2 offset, TextureChannel channel)
		{
			this.sheet = sheet;
			this.bounds = bounds;
			this.offset = offset;
			this.channel = channel;
			this.size = new float2(bounds.Size);

			var left = (float)(bounds.Left) / sheet.Size.Width;
			var top = (float)(bounds.Top) / sheet.Size.Height;
			var right = (float)(bounds.Right) / sheet.Size.Width;
			var bottom = (float)(bounds.Bottom) / sheet.Size.Height;
			textureCoords = new float2[]
			{
				new float2(left, top),
				new float2(right, top),
				new float2(left, bottom),
				new float2(right, bottom),
			};
		}

		public float2 FastMapTextureCoords(int k)
		{
			return textureCoords[k];
		}

		public void DrawAt(WorldRenderer wr, float2 location, string palette)
		{
			Game.Renderer.WorldSpriteRenderer.DrawSprite(this, location, wr, palette, size);
		}

		public void DrawAt(float2 location, int paletteIndex)
		{
			Game.Renderer.WorldSpriteRenderer.DrawSprite(this, location, paletteIndex, size);
		}

		public void DrawAt(float2 location, int paletteIndex, float scale)
		{
			Game.Renderer.WorldSpriteRenderer.DrawSprite(this, location, paletteIndex, size*scale);
		}

		public void DrawAt(float2 location, int paletteIndex, float2 size)
		{
			Game.Renderer.WorldSpriteRenderer.DrawSprite(this, location, paletteIndex, size);
		}
	}

	public enum TextureChannel
	{
		Red = 0,
		Green = 1,
		Blue = 2,
		Alpha = 3,
	}
}
