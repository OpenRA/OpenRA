using System.Drawing;
using OpenRA.FileFormats.Graphics;

namespace OpenRA.Renderer.Null
{
	public class NullTexture : ITexture
	{
		public void SetData(Bitmap bitmap)
		{
			
		}

		public void SetData(uint[,] colors)
		{
		}

		public void SetData(byte[] colors, int width, int height)
		{
		}
	}
}
