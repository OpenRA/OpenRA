using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	public delegate void Renderable(Renderer renderer, Viewport viewport);
	class Viewport
	{
		readonly Size clientSize;
		readonly float2 mapSize;
		float2 scrollPosition;
		readonly Renderer renderer;

		public PointF ScrollPosition { get { return scrollPosition.ToPointF(); } }
		public Size ClientSize { get { return clientSize; } }

		public void Scroll(float2 delta)
		{
			scrollPosition = (scrollPosition + delta).Constrain(new Range<float2>(float2.Zero, mapSize));
		}

		public Viewport(Size clientSize, float2 mapSize, Renderer renderer)
		{
			this.clientSize = clientSize;
			this.mapSize = 24 * mapSize - new float2(clientSize) + new float2(128, 0);
			this.renderer = renderer;
		}

		List<Region> regions = new List<Region>();
		public void ResquestRegion(AnchorStyles anchor, int distanceFromAnchor, Renderable drawFunction)
		{
			switch (anchor)
			{
				case AnchorStyles.Top:
					regions.Add(new Region(new PointF(0, 0), new Size(clientSize.Width, distanceFromAnchor), drawFunction));
					break;
				case AnchorStyles.Bottom:
					regions.Add(new Region(new PointF(0, clientSize.Height), new Size(clientSize.Width, distanceFromAnchor), drawFunction));
					break;
				case AnchorStyles.Left:
					regions.Add(new Region(new PointF(0, 0), new Size(distanceFromAnchor, clientSize.Height), drawFunction));
					break;
				case AnchorStyles.Right:
					regions.Add(new Region(new PointF(clientSize.Width, 0), new Size(distanceFromAnchor, clientSize.Height), drawFunction));
					break;
				case AnchorStyles.None:
					throw new NotImplementedException();
			}
		}

		public void DrawRegions()
		{
			foreach (Region region in regions)
				region.Draw(renderer, this);
		}
	}
}
