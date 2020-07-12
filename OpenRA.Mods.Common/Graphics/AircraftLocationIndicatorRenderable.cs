using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public struct AircraftLocationIndicatorRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos topPos;
		readonly WPos bottomPos;
		readonly int width;
		readonly Color topColor;
		readonly Color bottomColor;

		public AircraftLocationIndicatorRenderable(WPos topPos, WPos bottomPos, int width, Color topColor, Color bottomColor)
		{
			this.topPos = topPos;
			this.bottomPos = bottomPos;
			this.topColor = topColor;
			this.bottomColor = bottomColor;
			this.width = width;
		}

		WPos IRenderable.Pos { get { return bottomPos; } }
		PaletteReference IRenderable.Palette { get { return null; } }
		int IRenderable.ZOffset { get { return 0; } }
		bool IRenderable.IsDecoration { get { return true; } }

		IRenderable IRenderable.WithPalette(PaletteReference newPalette) { return this; }
		IRenderable IRenderable.WithZOffset(int newOffset) { return this; }
		IRenderable IRenderable.OffsetBy(WVec vec) { return new AircraftLocationIndicatorRenderable(topPos + vec, bottomPos + vec, width, topColor, bottomColor); }
		IRenderable IRenderable.AsDecoration() { return this; }
		IFinalizedRenderable IRenderable.PrepareRender(WorldRenderer wr) { return this; }

		void IFinalizedRenderable.Render(WorldRenderer wr)
		{
			var top = wr.Viewport.WorldToViewPx(wr.Screen3DPosition(topPos));
			var bottom = wr.Viewport.WorldToViewPx(wr.Screen3DPosition(bottomPos));

			var cr = Game.Renderer.RgbaColorRenderer;
			cr.DrawLine(top, bottom, width, topColor, bottomColor);
		}

		void IFinalizedRenderable.RenderDebugGeometry(WorldRenderer wr) { }
		Rectangle IFinalizedRenderable.ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
