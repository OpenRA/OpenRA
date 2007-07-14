using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class Viewport
	{
		readonly Size clientSize;
		readonly float2 mapSize;
		float2 scrollPosition;
		readonly Renderer renderer;

		public Size ClientSize { get { return clientSize; } }

		public float2 Location { get { return scrollPosition; } }
		public float2 Size { get { return new float2(ClientSize); } }

		public int Width { get { return clientSize.Width; } }
		public int Height { get { return clientSize.Height; } }

		public void Scroll(float2 delta)
		{
			scrollPosition = (scrollPosition + delta).Constrain(
				new Range<float2>(float2.Zero, mapSize));
		}

		public Viewport(Size clientSize, float2 mapSize, Renderer renderer)
		{
			this.clientSize = clientSize;
			this.mapSize = 24 * mapSize - new float2(clientSize) + new float2(128, 0);
			this.renderer = renderer;
		}

		List<Region> regions = new List<Region>();
		public void AddRegion(Region r)
		{
			regions.Add(r);
		}

		public void DrawRegions()
		{
			float2 r1 = new float2(2, -2) / Size;
			float2 r2 = new float2(-1, 1);
			
			renderer.BeginFrame(r1, r2, scrollPosition);

			foreach (Region region in regions)
				region.Draw(renderer, this);

			renderer.EndFrame();
		}
	}
}
