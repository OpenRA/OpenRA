using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class RevealingLabelWidget : LabelWidget
	{
		public int RevealLength = 0;
		public int RevealCyclesDelay = 3;
		public int CycleCount = 0;
		public RevealingLabelWidget() : base()
		{
			GetText = () => Text.Substring(0, RevealLength);
		}

		protected RevealingLabelWidget(RevealingLabelWidget other)
			: base(other)
		{
			RevealLength = other.RevealLength;
		}

		public override void Draw()
		{
			CycleCount++;
			if (RevealLength < Text.Length && CycleCount % RevealCyclesDelay == 0)
				RevealLength += 1;
			base.Draw();
		}

		public override Widget Clone() { return new RevealingLabelWidget(this); }
	}
}
