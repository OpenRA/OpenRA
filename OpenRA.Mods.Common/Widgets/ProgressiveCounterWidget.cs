using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ProgressiveCounterWidget : LabelWidget
	{
		public int Begin = 0;
		public int Step = 1;
		public int End = 0;
		public int Current = 0;
		public bool IsAnimationComplete = false;
		public bool PlaySound = true;
		public Action OnAnimationDone;

		public ProgressiveCounterWidget() 
			: base()
		{
			GetText = () => Current.ToString();
			OnAnimationDone = () => { };
		}

		protected ProgressiveCounterWidget(ProgressiveCounterWidget other)
			: base(other)
		{
			Begin = other.Begin;
			Step = other.Step;
			End = other.End;
			Current = other.Current;
			IsAnimationComplete = other.IsAnimationComplete;
			PlaySound = other.PlaySound;
			OnAnimationDone = other.OnAnimationDone;
		}

		public override void Draw()
		{
			if (!IsAnimationComplete)
			{
				Current += Step;
				if (Step > 0 && Current > End)
				{
					Current = End;
					IsAnimationComplete = true;
					OnAnimationDone();
				}
				else if (Step < 0 && Current < End)
				{
					Current = End;
					IsAnimationComplete = true;
					OnAnimationDone();
				}
				if (PlaySound)
					Game.Sound.Play(SoundType.UI, "beepy3.aud");
			}
			base.Draw();
		}

		public override Widget Clone() { return new ProgressiveCounterWidget(this); }
	}
}
