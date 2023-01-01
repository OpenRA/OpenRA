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
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class Sprite
	{
		public readonly Rectangle Bounds;
		public readonly Sheet Sheet;
		public readonly BlendMode BlendMode;
		public readonly TextureChannel Channel;
		public readonly float ZRamp;
		public readonly float3 Size;
		public readonly float3 Offset;
		public readonly float Top, Left, Bottom, Right;

		public Sprite(Sheet sheet, Rectangle bounds, TextureChannel channel, float scale = 1)
			: this(sheet, bounds, 0, float2.Zero, channel, BlendMode.Alpha, scale) { }

		public Sprite(Sheet sheet, Rectangle bounds, float zRamp, in float3 offset, TextureChannel channel, BlendMode blendMode = BlendMode.Alpha, float scale = 1f)
		{
			Sheet = sheet;
			Bounds = bounds;
			Offset = offset;
			ZRamp = zRamp;
			Channel = channel;
			Size = scale * new float3(bounds.Size.Width, bounds.Size.Height, bounds.Size.Height * zRamp);
			BlendMode = blendMode;

			// Some GPUs suffer from precision issues when rendering into non 1:1 framebuffers that result
			// in rendering a line of texels that sample outside the sprite rectangle.
			// Insetting the texture coordinates by a small fraction of a pixel avoids this
			// with negligible impact on the 1:1 rendering case.
			var inset = 1 / 128f;
			Left = (Math.Min(bounds.Left, bounds.Right) + inset) / sheet.Size.Width;
			Top = (Math.Min(bounds.Top, bounds.Bottom) + inset) / sheet.Size.Height;
			Right = (Math.Max(bounds.Left, bounds.Right) - inset) / sheet.Size.Width;
			Bottom = (Math.Max(bounds.Top, bounds.Bottom) - inset) / sheet.Size.Height;
		}
	}

	public class SpriteWithSecondaryData : Sprite
	{
		public readonly Sheet SecondarySheet;
		public readonly Rectangle SecondaryBounds;
		public readonly TextureChannel SecondaryChannel;
		public readonly float SecondaryTop, SecondaryLeft, SecondaryBottom, SecondaryRight;

		public SpriteWithSecondaryData(Sprite s, Sheet secondarySheet, Rectangle secondaryBounds, TextureChannel secondaryChannel)
			: base(s.Sheet, s.Bounds, s.ZRamp, s.Offset, s.Channel, s.BlendMode)
		{
			SecondarySheet = secondarySheet;
			SecondaryBounds = secondaryBounds;
			SecondaryChannel = secondaryChannel;
			SecondaryLeft = (float)Math.Min(secondaryBounds.Left, secondaryBounds.Right) / s.Sheet.Size.Width;
			SecondaryTop = (float)Math.Min(secondaryBounds.Top, secondaryBounds.Bottom) / s.Sheet.Size.Height;
			SecondaryRight = (float)Math.Max(secondaryBounds.Left, secondaryBounds.Right) / s.Sheet.Size.Width;
			SecondaryBottom = (float)Math.Max(secondaryBounds.Top, secondaryBounds.Bottom) / s.Sheet.Size.Height;
		}
	}

	public enum TextureChannel : byte
	{
		Red = 0,
		Green = 1,
		Blue = 2,
		Alpha = 3,
		RGBA = 4
	}
}
