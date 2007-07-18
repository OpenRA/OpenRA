using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using BluntDirectX.Direct3D;
using System.IO;
using System.Drawing.Imaging;

namespace OpenRa.Game
{
	class Sheet
	{
		public readonly Bitmap bitmap;
		
		readonly GraphicsDevice device;
		Texture texture;

		string filename = string.Format("../../../block-cache-{0}.png", suffix++);

		public Size Size { get { return bitmap.Size; } }

		public Sheet(Size size, GraphicsDevice d)
		{
			bitmap = new Bitmap(size.Width, size.Height);
			device = d;
		}

		public Texture Texture
		{
			get
			{
				if (texture == null)
					LoadTexture();

				return texture;
			}
		}

		void LoadTexture()
		{
			bitmap.Save(filename);

			using( Stream s = File.OpenRead(filename) )
				texture = Texture.Create(s, device);
		}

		static int suffix = 0;
	}
}
