using System.Collections.Generic;
using System;
using System.Linq;

namespace OpenRa.Game.Graphics
{
	class Viewport
	{
		readonly float2 screenSize;
		float2 scrollPosition;
		readonly Renderer renderer;

		public float2 Location { get { return scrollPosition; } }

		public int Width { get { return (int)screenSize.X; } }
		public int Height { get { return (int)screenSize.Y; } }

		public Cursor cursor = Cursor.Move;
		SpriteRenderer cursorRenderer;
		int2 mousePos;
		float cursorFrame = 0f;

		readonly float2 scrollLowBounds, scrollHighBounds;

		public void Scroll(float2 delta)
		{
			scrollPosition = ( scrollPosition + delta ).Constrain( scrollLowBounds, scrollHighBounds );
		}

		public Viewport(float2 screenSize, int2 mapStart, int2 mapEnd, Renderer renderer)
		{
			this.screenSize = screenSize;
			this.scrollLowBounds = Game.CellSize * mapStart;
			this.scrollHighBounds = float2.Max( scrollLowBounds, Game.CellSize * mapEnd - ( screenSize - new float2( 128, 0 ) ) );
			this.renderer = renderer;
			cursorRenderer = new SpriteRenderer(renderer, true);

			this.scrollPosition = scrollLowBounds;
		}

		List<Region> regions = new List<Region>();

		public void AddRegion(Region r) { regions.Add(r); }

		public void DrawRegions()
		{
			float2 r1 = new float2(2, -2) / screenSize;
			float2 r2 = new float2(-1, 1);
			
			renderer.BeginFrame(r1, r2, scrollPosition);

			foreach (Region region in regions)
				region.Draw(renderer);

			var c = (Game.worldRenderer.region.Contains(mousePos)) ? cursor : Cursor.Default;
			cursorRenderer.DrawSprite(c.GetSprite((int)cursorFrame), mousePos + Location - c.GetHotspot(), 0);
			cursorRenderer.Flush();

			renderer.EndFrame();
		}

		public void Tick()
		{
			cursorFrame += 0.5f;
		}

        Region dragRegion = null;
        public void DispatchMouseInput(MouseInput mi)
        {
			if (mi.Event == MouseInputEvent.Move)
				mousePos = mi.Location;

            if (dragRegion != null) {
                dragRegion.HandleMouseInput( mi );
                if (mi.Event == MouseInputEvent.Up) dragRegion = null;
                return;
            }

			if (mi.Event == MouseInputEvent.Move)
				foreach (var reg in regions.Where(r => r.AlwaysWantMovement))
					reg.HandleMouseInput(mi);

            dragRegion = regions.FirstOrDefault(r => r.Contains(mi.Location) && r.HandleMouseInput(mi));
            if (mi.Event != MouseInputEvent.Down)
                dragRegion = null;
        }

		public float2 ViewToWorld(MouseInput mi)
		{
			return (1 / 24.0f) * (new float2(mi.Location.X, mi.Location.Y) + Location);
		}
	}
}
