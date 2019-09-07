using System;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class RevealingLabelWidget : LabelWidget
	{
		public int RevealLength = 0;
		public int RevealStep = 1;
		public int RevealCyclesDelay = 1;
		public Color RevealColor = Color.White;
		public int CycleCount = 0;
		public bool IsAnimationComplete = false;
		public bool PlaySound = true;
		public Action OnAnimationDone; 

		public RevealingLabelWidget() 
			: base()
		{
			GetText = () => Text.Substring(0, RevealLength);
			OnAnimationDone = () => { };
		}

		protected RevealingLabelWidget(RevealingLabelWidget other)
			: base(other)
		{
			RevealLength = other.RevealLength;
			OnAnimationDone = other.OnAnimationDone;
		}

		Color GetRevealColor()
		{
			return RevealColor;
		}

		Color GetTextColor()
		{
			return TextColor;
		}

		public override void Draw()
		{
			if (!IsAnimationComplete)
			{
				GetColor = GetRevealColor;
				CycleCount++;
				if (PlaySound)
					Game.Sound.Play(SoundType.UI, "keystrok.aud");
				if (CycleCount % RevealCyclesDelay == 0)
					RevealLength += RevealStep;
				RevealLength++;
				if (RevealLength >= Text.Length)
				{
					IsAnimationComplete = true;
					RevealLength = Text.Length;
					OnAnimationDone();
					GetColor = GetTextColor;
				}

			}
			base.Draw();
			if (!IsAnimationComplete)
			{
				RevealLength--;
				GetColor = GetTextColor;
				base.Draw();
			}
		}

		public override Widget Clone() { return new RevealingLabelWidget(this); }
	}
}
