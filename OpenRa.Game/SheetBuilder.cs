using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	delegate T Provider<T>();

	static class SheetBuilder
	{
		public static void Initialize(GraphicsDevice device)
		{
			pageProvider = delegate { return new Sheet(new Size(512,512), device); };
		}

		public static Sprite Add(byte[] src, Size size)
		{
			Sprite rect = AddImage(size);
			Util.CopyIntoChannel(rect, src);
			return rect;
		}

		public static Sprite Add(Size size, byte paletteIndex)
		{
			byte[] data = new byte[size.Width * size.Height];
			for (int i = 0; i < data.Length; i++)
				data[i] = paletteIndex;

			return Add(data, size);
		}

		static Provider<Sheet> pageProvider;
		static Sheet current = null;
		static int x = 0, y = 0, rowHeight = 0;
		static TextureChannel? channel = null;

		static TextureChannel? NextChannel(TextureChannel? t)
		{
			if (t == null)
				return TextureChannel.Red;

			switch (t.Value)
			{
				case TextureChannel.Red: return TextureChannel.Green;
				case TextureChannel.Green: return TextureChannel.Blue;
				case TextureChannel.Blue: return TextureChannel.Alpha;
				case TextureChannel.Alpha: return null;

				default: return null;
			}
		}

		static Sprite AddImage(Size imageSize)
		{
			if (current == null)
			{
				current = pageProvider();
				channel = NextChannel(null);
			}

			if (rowHeight == 0 || imageSize.Width + x > current.Size.Width)
			{
				y += rowHeight;
				rowHeight = imageSize.Height;
				x = 0;
			}

			if (imageSize.Height > rowHeight)
				rowHeight = imageSize.Height;

			if (y + imageSize.Height > current.Size.Height)
			{

				if (null == (channel = NextChannel(channel)))
				{
					current = pageProvider();
					channel = NextChannel(channel);
				}

				x = y = rowHeight = 0;
			}

			Sprite rect = new Sprite(current, new Point(x, y), imageSize, channel.Value);
			x += imageSize.Width;

			return rect;
		}
	}
}
