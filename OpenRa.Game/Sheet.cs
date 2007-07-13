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

		public Sheet(Size size, GraphicsDevice d)
		{
			bitmap = new Bitmap(size.Width, size.Height);
			device = d;

			using (Graphics g = Graphics.FromImage(bitmap))
				g.FillRectangle(Brushes.Fuchsia, 0, 0, size.Width, size.Height);
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
			string tempFile = string.Format("../../../block-cache-{0}.png", suffix++);
			bitmap.Save(tempFile);

			using( Stream s = File.OpenRead(tempFile) )
				texture = Texture.Create(s, device);
		}

		static int suffix = 0;
	}
}
