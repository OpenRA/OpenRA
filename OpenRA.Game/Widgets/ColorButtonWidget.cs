
using System;
using System.Drawing;

namespace OpenRA.Widgets
{
	class ColorButtonWidget : ButtonWidget 
	{
		public int PaletteIndex = 0;
		public Func<int> GetPaletteIndex;
		
		public ColorButtonWidget()
			: base()
		{
			GetPaletteIndex = () => { return PaletteIndex; };
		}
		
		public ColorButtonWidget(Widget widget)
			:base(widget)
		{
			PaletteIndex = (widget as ColorButtonWidget).PaletteIndex;
			GetPaletteIndex = (widget as ColorButtonWidget).GetPaletteIndex;
		}
		
		public override Widget Clone()
		{	
			return new ColorButtonWidget(this);
		}
		
		public override void Draw(World world)
		{
			if (!IsVisible())
			{
				base.Draw(world);
				return;
			}
			
			DrawColorBlock();
			
			base.Draw(world);
		}
	
		void DrawColorBlock()	
		{
			var pos = DrawPosition();
			var paletteRect = new Rectangle(pos.X ,pos.Y + 2 , 65, 22);
			Game.chrome.lineRenderer.FillRect(RectangleF.FromLTRB(paletteRect.Left + Game.viewport.Location.X + 5,
															paletteRect.Top + Game.viewport.Location.Y + 5,
															paletteRect.Right + Game.viewport.Location.X - 5,
															paletteRect.Bottom+Game.viewport.Location.Y - 5),
													Player.PlayerColors(Game.world)[GetPaletteIndex() % Player.PlayerColors(Game.world).Count].c);
		}
	}
}
