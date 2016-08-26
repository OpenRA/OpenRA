#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	[Desc("Visualize the remaining CaptureCompleteTime from ExternalCapturable: trait.")]
	class ExternalCapturableBarInfo : ITraitInfo, Requires<ExternalCapturableInfo>
	{
		public object Create(ActorInitializer init) { return new ExternalCapturableBar(init.Self); }
	}

	class ExternalCapturableBar : ISelectionBar
	{
		readonly ExternalCapturable capturable;

		public ExternalCapturableBar(Actor self)
		{
			capturable = self.Trait<ExternalCapturable>();
		}

		float ISelectionBar.GetValue()
		{
			// only show when building is being captured
			if (!capturable.CaptureInProgress)
				return 0f;

			return (float)capturable.CaptureProgressTime / (capturable.Info.CaptureCompleteTime * 25);
		}

		Color ISelectionBar.GetColor() { return Color.Orange; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
