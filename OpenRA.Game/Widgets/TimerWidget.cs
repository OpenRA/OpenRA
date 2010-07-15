using System;
using System.Drawing;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	public class TimerWidget : Widget
	{
		public Stopwatch Stopwatch;
		
		public TimerWidget ()
		{
			Stopwatch = new Stopwatch();
			IsVisible = () => Game.Settings.ShowGameTimer;
		}
		
		public override void DrawInner (World world)
		{			
			var s = WorldUtils.FormatTime((int) Stopwatch.ElapsedTime() * 25);
			var size = Game.chrome.renderer.RegularFont.Measure(s);
			var padding = 5;
			WidgetUtils.DrawPanel("dialog4",new Rectangle(RenderBounds.Top - padding, RenderBounds.Left - padding, size.X + 2*padding, size.Y + 2*padding));
			Game.chrome.renderer.RegularFont.DrawText(s, new float2(RenderBounds.Top, RenderBounds.Left), Color.White);
		}
	}
}

