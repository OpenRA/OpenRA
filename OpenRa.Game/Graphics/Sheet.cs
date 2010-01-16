using System.Drawing;
using Ijw.DirectX;
using OpenRa.FileFormats;

namespace OpenRa.Graphics
{
	public class Sheet
	{
		readonly Renderer renderer;
		protected readonly Bitmap bitmap;

		Texture texture;

		internal Sheet(Renderer renderer, Size size)
		{
			this.renderer = renderer;
			this.bitmap = new Bitmap(size.Width, size.Height);
		}

		internal Sheet(Renderer renderer, string filename)
		{
			this.renderer = renderer;
			this.bitmap = (Bitmap)Image.FromStream(FileSystem.Open(filename));
		}

		void Resolve()
		{
			texture = Texture.CreateFromBitmap(bitmap, renderer.Device);
		}

		public Texture Texture
		{
			get
			{
				if (texture == null)
					Resolve();

				return texture;
			}
		}

		public Size Size { get { return bitmap.Size; } }

		protected Color this[Point p]
		{
			get { return bitmap.GetPixel(p.X, p.Y); }
			set { bitmap.SetPixel(p.X, p.Y, value); }
		}

		public Bitmap Bitmap { get { return bitmap; } }	// for perf
	}
}
