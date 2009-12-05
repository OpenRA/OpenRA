using System.Drawing;

namespace OpenRa.FileFormats
{
	public struct TreeReference
	{
		public readonly int X;
		public readonly int Y;
		public readonly string Image;

		public TreeReference(int xy, string image)
		{
			X = xy % 128;
			Y = xy / 128;
			Image = image;
		}

		public Point Location { get { return new Point(X, Y); } }
	}
}
