#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
		public readonly BlendMode blendMode;
		public readonly TextureChannel channel;
		public readonly float2 size;
		public readonly float2 offset;
		public readonly float2 fractionalOffset;
		public readonly float top, left, bottom, right;

		public Sprite(Sheet sheet, Rectangle bounds, TextureChannel channel)
			: this(sheet, bounds, float2.Zero, channel, BlendMode.Alpha) {}

		public Sprite(Sheet sheet, Rectangle bounds, TextureChannel channel, BlendMode blendMode)
			: this(sheet, bounds, float2.Zero, channel, blendMode) {}

		public Sprite(Sheet sheet, Rectangle bounds, float2 offset, TextureChannel channel, BlendMode blendMode)
		{
			this.sheet = sheet;
			this.bounds = bounds;
			this.offset = offset;
			this.channel = channel;
			this.size = new float2(bounds.Size);
			this.blendMode = blendMode;

			this.fractionalOffset = offset / this.size;

			left = (float)(bounds.Left) / sheet.Size.Width;
			top = (float)(bounds.Top) / sheet.Size.Height;
			right = (float)(bounds.Right) / sheet.Size.Width;
			bottom = (float)(bounds.Bottom) / sheet.Size.Height;
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
