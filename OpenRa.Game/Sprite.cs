using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.Game
{
	class Sprite
	{
		public readonly Rectangle bounds;
		public readonly Sheet sheet;
		public readonly TextureChannel channel;

		internal Sprite(Sheet sheet, Rectangle bounds, TextureChannel channel)
		{
			this.bounds = bounds;
			this.sheet = sheet;
			this.channel = channel;
		}

		RectangleF TextureCoords
		{
			get
			{
				return new RectangleF(
					(float)(bounds.Left + 0.5f) / sheet.Size.Width,
					(float)(bounds.Top + 0.5f) / sheet.Size.Height,
					(float)(bounds.Width) / sheet.Size.Width,
					(float)(bounds.Height) / sheet.Size.Height);
			}
		}

		public float2 MapTextureCoords(float2 p)
		{
			RectangleF uv = TextureCoords;

			return new float2(
				p.X > 0 ? uv.Right : uv.Left,
				p.Y > 0 ? uv.Bottom : uv.Top);
		}
	}

	public enum TextureChannel
	{
		Red,
		Green,
		Blue,
		Alpha,
	}
}
