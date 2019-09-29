using System;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ProgressiveBarCounterWidget : ProgressBarWidget
	{
		public Color TextColor = ChromeMetrics.Get<Color>("TextColor");
		public string Font = ChromeMetrics.Get<string>("TextFont");
		public int MaxTicks = 75;
		public int MaxValue
		{
			get
			{
				return counter.End;
			}
			set
			{
				if (value == 0)
					counter.PlaySound = false;
				counter.End = value;
				counter.Step = value > 0 ? value / MaxTicks : 1;
			}
		}

		public Action OnAnimationDone
		{
			get { return counter.OnAnimationDone; }
			set { counter.OnAnimationDone = value; }
		}

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

		public override void Draw()
		{
			base.Draw();
			counter.Bounds = Bounds;
			counter.X = X;
			counter.Y = Y;
			counter.Width = Width;
			counter.Height = Height;
			counter.Parent = Parent;
			counter.Visible = Visible;
			counter.Draw();
		}

		public override Widget Clone() { return new ProgressiveBarCounterWidget(this); }

		public override void Hidden()
		{
			counter.Hidden();
			base.Hidden();
		}

		public override void Removed()
		{
			counter.Removed();
			base.Removed();
		}
	}
}
