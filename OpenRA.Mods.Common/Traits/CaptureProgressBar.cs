#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Visualize capture progress.")]
	class CapturableProgressBarInfo : ITraitInfo, Requires<CapturableInfo>
	{
		public readonly Color Color = Color.Orange;

		public object Create(ActorInitializer init) { return new CapturableProgressBar(init.Self, this); }
	}

	class CapturableProgressBar : ISelectionBar, ICaptureProgressWatcher
	{
		readonly CapturableProgressBarInfo info;
		Dictionary<Actor, Pair<int, int>> progress = new Dictionary<Actor, Pair<int, int>>();

		public CapturableProgressBar(Actor self, CapturableProgressBarInfo info)
		{
			this.info = info;
		}

		void ICaptureProgressWatcher.Update(Actor self, Actor captor, Actor target, int current, int total)
		{
			if (self != target)
				return;

			if (total == 0)
				progress.Remove(captor);
			else
				progress[captor] = Pair.New(current, total);
		}

		float ISelectionBar.GetValue()
		{
			if (!progress.Any())
				return 0f;

			return progress.Values.Max(p => (float)p.First / p.Second);
		}

		Color ISelectionBar.GetColor() { return info.Color; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
