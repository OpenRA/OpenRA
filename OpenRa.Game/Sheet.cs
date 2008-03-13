using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using Ijw.DirectX;

namespace OpenRa.Game
{
	class Sheet
	{
		readonly Renderer renderer;
		protected readonly Bitmap bitmap;

		Texture texture;
		static int suffix = 0;

		public Sheet(Renderer renderer, Size size)
		{
			this.renderer = renderer;
			this.bitmap = new Bitmap(size.Width, size.Height);
		}

		void Resolve()
		{
			string filename = string.Format("../../../sheet-{0}.png", suffix++);
			bitmap.Save(filename);

			using (Stream s = File.OpenRead(filename))
				texture = Texture.Create(s, renderer.Device);
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

		public Color this[Point p]
		{
			get { return bitmap.GetPixel(p.X, p.Y); }
			set { bitmap.SetPixel(p.X, p.Y, value); }
		}

		public Bitmap Bitmap { get { return bitmap; } }	// for perf
	}
}
