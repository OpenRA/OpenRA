using System.Drawing;

namespace OpenRa.Game.Graphics
{
	static class SheetBuilder
	{
		public static void Initialize(Renderer r)
		{
			renderer = r;
		}

		public static Sprite Add(byte[] src, Size size)
		{
			Sprite rect = AddImage(size);
			Util.FastCopyIntoChannel(rect, src);
			return rect;
		}

		public static Sprite Add(Size size, byte paletteIndex)
		{
			byte[] data = new byte[size.Width * size.Height];
			for (int i = 0; i < data.Length; i++)
				data[i] = paletteIndex;

			return Add(data, size);
		}

		static Sheet NewSheet() { return new Sheet( renderer, new Size( Renderer.SheetSize, Renderer.SheetSize ) ); }

		static Renderer renderer;
		static Sheet current = null;
		static int rowHeight = 0;
		static Point p;
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
				current = NewSheet();
				channel = NextChannel(null);
			}

			if (imageSize.Width + p.X > current.Size.Width)
			{
				p = new Point(0, p.Y + rowHeight);
				rowHeight = imageSize.Height;
			}

			if (imageSize.Height > rowHeight)
				rowHeight = imageSize.Height;

			if (p.Y + imageSize.Height > current.Size.Height)
			{

				if (null == (channel = NextChannel(channel)))
				{
					current = NewSheet();
					channel = NextChannel(channel);
				}

				rowHeight = imageSize.Height;
				p = new Point(0,0);
			}

			Sprite rect = new Sprite(current, new Rectangle(p, imageSize), channel.Value);
			p.X += imageSize.Width;

			return rect;
		}
	}
}
