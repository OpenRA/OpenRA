#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Visualize the progress of this actor being captured.")]
	class CaptureProgressBarInfo : ITraitInfo, Requires<CapturesInfo>
	{
		public readonly Color Color = Color.Orange;

		public object Create(ActorInitializer init) { return new CaptureProgressBar(init.Self, this); }
	}

	class CaptureProgressBar : ISelectionBar, ICaptureProgressWatcher
	{
		readonly CaptureProgressBarInfo info;
		int current;
		int total;

		public CaptureProgressBar(Actor self, CaptureProgressBarInfo info)
		{
			this.info = info;
		}

		void ICaptureProgressWatcher.Update(Actor self, Actor captor, Actor target, int current, int total)
		{
			if (self != captor)
				return;

			this.current = current;
			this.total = total;
		}

		float ISelectionBar.GetValue()
		{
			if (total == 0)
				return 0f;

			return (float)current / total;
		}

		Color ISelectionBar.GetColor() { return info.Color; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
