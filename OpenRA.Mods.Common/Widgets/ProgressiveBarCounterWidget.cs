using System;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ProgressiveBarCounterWidget : ProgressBarWidget
	{
		int MaxValue = 1;
		public Color TextColor = ChromeMetrics.Get<Color>("TextColor");
		public string Font = ChromeMetrics.Get<string>("TextFont");
		ProgressiveCounterWidget counter;

		public ProgressiveBarCounterWidget() 
			: base()
		{
			counter = new ProgressiveCounterWidget
			{
				TextColor = TextColor,
				Font = Font,
				Visible = Visible,
				X = X,
				Y = Y,
				Width = Width,
				Height = Height,
				Parent = Parent,
				Align = TextAlign.Center
			};
			GetPercentage = () => (int)(((float)counter.Current / MaxValue) * 100);
		}

		protected ProgressiveBarCounterWidget(ProgressiveBarCounterWidget other)
			: base(other)
		{
			MaxValue = other.MaxValue;
		}

		public void SetMaxValue(int maxValue)
		{
			counter.End = maxValue;
			MaxValue = maxValue;
		}

		public override void Draw()
		{
			base.Draw();
			counter.Bounds = base.Bounds;
			counter.X = base.X;
			counter.Y = base.Y;
			counter.Width = base.Width;
			counter.Height = base.Height;
			counter.Parent = base.Parent;
			counter.Visible = Visible;
			counter.Draw();
		}

		public override Widget Clone() { return new ProgressiveBarCounterWidget(this); }
	}
}
