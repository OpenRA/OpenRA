#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("Visualize the remaining CaptureCompleteTime from ExternalCapturable: trait.")]
	class ExternalCapturableBarInfo : ITraitInfo, Requires<ExternalCapturableInfo>
	{
		public object Create(ActorInitializer init) { return new ExternalCapturableBar(init.self); }
	}

	class ExternalCapturableBar : ISelectionBar
	{
		ExternalCapturable cap;

		public ExternalCapturableBar(Actor self)
		{
			this.cap = self.Trait<ExternalCapturable>();
		}

		public float GetValue()
		{
			// only show when building is being captured
			if (!cap.CaptureInProgress)
				return 0f;

			return (float)cap.CaptureProgressTime / (cap.Info.CaptureCompleteTime * 25);
		}
		public Color GetColor() { return Color.Orange; }
	}
}
