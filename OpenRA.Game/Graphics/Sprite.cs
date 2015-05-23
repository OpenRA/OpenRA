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
		public readonly Rectangle Bounds;
		public readonly Sheet Sheet;
		public readonly BlendMode BlendMode;
		public readonly TextureChannel Channel;
		public readonly float2 Size;
		public readonly float2 Offset;
		public readonly float2 FractionalOffset;
		public readonly float Top, Left, Bottom, Right;

		public Sprite(Sheet sheet, Rectangle bounds, TextureChannel channel)
			: this(sheet, bounds, float2.Zero, channel, BlendMode.Alpha) { }

		public Sprite(Sheet sheet, Rectangle bounds, TextureChannel channel, BlendMode blendMode)
			: this(sheet, bounds, float2.Zero, channel, blendMode) { }

		public Sprite(Sheet sheet, Rectangle bounds, float2 offset, TextureChannel channel, BlendMode blendMode)
		{
			Sheet = sheet;
			Bounds = bounds;
			Offset = offset;
			Channel = channel;
			Size = new float2(bounds.Size);
			BlendMode = blendMode;

			FractionalOffset = offset / Size;

			Left = (float)bounds.Left / sheet.Size.Width;
			Top = (float)bounds.Top / sheet.Size.Height;
			Right = (float)bounds.Right / sheet.Size.Width;
			Bottom = (float)bounds.Bottom / sheet.Size.Height;
		}
	}

	public enum TextureChannel : byte
	{
		Red = 0,
		Green = 1,
		Blue = 2,
		Alpha = 3,
	}
}
