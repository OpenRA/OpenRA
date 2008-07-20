using System.Collections.Generic;

namespace OpenRa.Game.Graphics
{
	class Viewport
	{
		readonly float2 size;
		readonly float2 mapSize;
		float2 scrollPosition;
		readonly Renderer renderer;

		public float2 Location { get { return scrollPosition; } }
		public float2 Size { get { return size; } }

		public int Width { get { return (int)size.X; } }
		public int Height { get { return (int)size.Y; } }

		public void Scroll(float2 delta)
		{
			scrollPosition = (scrollPosition + delta).Constrain(float2.Zero, mapSize);
		}

		public Viewport(float2 size, float2 mapSize, Renderer renderer)
		{
			this.size = size;
			this.mapSize = 24 * mapSize - size + new float2(128, 0);
			this.renderer = renderer;
		}

		List<Region> regions = new List<Region>();
		public void AddRegion(Region r)
		{
			regions.Add(r);
		}

		public void DrawRegions(Game game)
		{
			float2 r1 = new float2(2, -2) / Size;
			float2 r2 = new float2(-1, 1);
			
			renderer.BeginFrame(r1, r2, scrollPosition);

			foreach (Region region in regions)
				region.Draw(renderer);

			renderer.EndFrame();
		}

		public IEnumerable<Region> Regions
		{
			get { return regions; }
		}
	}
}
