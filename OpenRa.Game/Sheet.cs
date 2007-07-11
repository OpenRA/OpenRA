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
		public Texture texture;

		public Sheet(Bitmap b) { bitmap = b; }

		public void LoadTexture(GraphicsDevice device)
		{
			string tempFile = string.Format("../../../block-cache-{0}.png", suffix++);
			bitmap.Save(tempFile);

			using( Stream s = File.OpenRead(tempFile) )
				texture = Texture.Create(s, device);
		}

		static int suffix = 0;
	}
}
