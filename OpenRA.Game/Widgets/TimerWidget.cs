using System.Drawing;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	public class TimerWidget : Widget
	{
		public Stopwatch Stopwatch;
		
		public TimerWidget ()
		{
			IsVisible = () => Game.Settings.ShowGameTimer;
		}

		public override void DrawInner(World world)
		{
			var s = WorldUtils.FormatTime(Game.LocalTick);
			var f = Game.chrome.renderer.TitleFont;
			var size = f.Measure(s);
//			var padding = 5;
//			WidgetUtils.DrawPanel("dialog4", new Rectangle(RenderBounds.Left - padding, RenderBounds.Top - padding, size.X + 2 * padding, size.Y + 2 * padding));
			f.DrawText(s, new float2(RenderBounds.Left - size.X / 2, RenderBounds.Top - 20), Color.White);
		}
	}
}

