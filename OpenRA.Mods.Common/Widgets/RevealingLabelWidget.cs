using System;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class RevealingLabelWidget : LabelWidget
	{
		public int RevealLength = 0;
		public int RevealStep = 1;
		public int RevealCyclesDelay = 3;
		public Color RevealColor = Color.White;
		public enum REVEAL_TYPES { BLANK, UNDERLINE, BLOCK }
		public REVEAL_TYPES RevealType = REVEAL_TYPES.UNDERLINE;
		public int CycleCount = 0;
		public bool IsAnimationComplete = false;
		public bool PlaySound = false;
		public Action OnAnimationDone;
		ISound revealSound;
		bool revealDraw = false;

		public RevealingLabelWidget()
			: base()
		{
			GetText = () =>
			{
				if (IsAnimationComplete)
					return Text;
				else
				{
					if (revealDraw)
						switch (RevealType)
						{
							case REVEAL_TYPES.UNDERLINE:
								return Text.Substring(0, RevealLength) + '_';
							case REVEAL_TYPES.BLOCK:
								return Text.Substring(0, RevealLength) + '\u2588';
							case REVEAL_TYPES.BLANK:
							default:
								return Text.Substring(0, RevealLength);
						}
					else
						return Text.Substring(0, RevealLength - 1 < 0 ? RevealLength : RevealLength - 1);
				}
			};
			GetColor = () =>
			{
				if (revealDraw)
					return RevealColor;
				else
					return TextColor;
			};
			OnAnimationDone = () =>
			{
				if (revealSound != null)
					Game.Sound.StopSound(revealSound);
			};
		}

		protected RevealingLabelWidget(RevealingLabelWidget other)
			: base(other)
		{
			RevealLength = other.RevealLength;
			OnAnimationDone = other.OnAnimationDone;
			IsVisible = other.IsVisible;
		}

		public override void Draw()
		{
			if (CycleCount == 0 && PlaySound)
				revealSound = Game.Sound.PlayLooped(SoundType.UI, "beepy2.aud");
			if (!IsAnimationComplete)
			{
				revealDraw = true;
				base.Draw();
				revealDraw = false;
				CycleCount++;
				if (CycleCount % RevealCyclesDelay == 0)
					RevealLength += RevealStep + 1;
				if (RevealLength >= Text.Length)
				{
					IsAnimationComplete = true;
					OnAnimationDone();
					if (revealSound != null)
					{
						Game.Sound.StopSound(revealSound);
						revealSound = null;
					}
				}
			}

			base.Draw();
		}

		public override Widget Clone() { return new RevealingLabelWidget(this); }

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
