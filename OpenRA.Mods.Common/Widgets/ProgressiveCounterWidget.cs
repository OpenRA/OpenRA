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
		public bool PlaySound = false;
		public Action OnAnimationDone;
		ISound revealSound;

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
			IsVisible = other.IsVisible;
		}

		public override void Draw()
		{
			if (Current == 0 && PlaySound)
				revealSound = Game.Sound.PlayLooped(SoundType.UI, "beepy3.aud");
			if (!IsAnimationComplete)
			{
				Current += Step;
				if (Step > 0 && Current > End)
				{
					Current = End;
					IsAnimationComplete = true;
					OnAnimationDone();
					Game.Sound.StopSound(revealSound);
				}
				else if (Step < 0 && Current < End)
				{
					Current = End;
					IsAnimationComplete = true;
					OnAnimationDone();
					Game.Sound.StopSound(revealSound);
				}
			}

			base.Draw();
		}

		public override Widget Clone() { return new ProgressiveCounterWidget(this); }

		public override void Hidden()
		{
			if (PlaySound && revealSound != null)
			{
				Game.Sound.StopSound(revealSound);
				revealSound = null;
			}

			base.Hidden();
		}

		public override void Removed()
		{
			if (PlaySound && revealSound != null)
			{
				Game.Sound.StopSound(revealSound);
				revealSound = null;
			}

			base.Removed();
		}
	}
}
