﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CapturableBarInfo : ITraitInfo, Requires<CapturableInfo>
	{
		public object Create(ActorInitializer init) { return new CapturableBar(init.self); }
	}

	class CapturableBar : ISelectionBar
	{
		Capturable cap;

		public CapturableBar(Actor self)
		{
			this.cap = self.Trait<Capturable>();
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
