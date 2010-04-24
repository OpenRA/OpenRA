
using System;
using System.Drawing;

namespace OpenRA.Widgets
{
	class ColorBlockWidget : Widget 
	{
		public int PaletteIndex = 0;
		public Func<int> GetPaletteIndex;
		
		public ColorBlockWidget()
			: base()
		{
			GetPaletteIndex = () => { return PaletteIndex; };
		}
		
		public ColorBlockWidget(Widget widget)
			:base(widget)
		{
			PaletteIndex = (widget as ColorBlockWidget).PaletteIndex;
			GetPaletteIndex = (widget as ColorBlockWidget).GetPaletteIndex;
		}
		
		public override Widget Clone()
		{	
			return new ColorBlockWidget(this);
		}
		
		public override void Draw(World world)
		{
			if (!IsVisible())
			{
				base.Draw(world);
				return;
			}
			
			var pos = DrawPosition();
			var paletteRect = new RectangleF(pos.X + Game.viewport.Location.X, pos.Y + Game.viewport.Location.Y, Bounds.Width, Bounds.Height);
			Game.chrome.lineRenderer.FillRect(paletteRect, Player.PlayerColors(Game.world)[GetPaletteIndex() % Player.PlayerColors(Game.world).Count].c);
			
			base.Draw(world);
		}
	}
}
