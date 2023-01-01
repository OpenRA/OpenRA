#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Visualize capture progress.")]
	class CapturableProgressBarInfo : ConditionalTraitInfo, Requires<CapturableInfo>
	{
		public readonly Color Color = Color.Orange;

		public override object Create(ActorInitializer init) { return new CapturableProgressBar(this); }
	}

	class CapturableProgressBar : ConditionalTrait<CapturableProgressBarInfo>, ISelectionBar, ICaptureProgressWatcher
	{
		readonly Dictionary<Actor, (int Current, int Total)> progress = new Dictionary<Actor, (int, int)>();

		public CapturableProgressBar(CapturableProgressBarInfo info)
			: base(info) { }

		void ICaptureProgressWatcher.Update(Actor self, Actor captor, Actor target, int current, int total)
		{
			if (self != target)
				return;

			if (total == 0)
				progress.Remove(captor);
			else
				progress[captor] = (current, total);
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || progress.Count == 0)
				return 0f;

			return progress.Values.Max(p => (float)p.Current / p.Total);
		}

		Color ISelectionBar.GetColor() { return Info.Color; }
		bool ISelectionBar.DisplayWhenEmpty => false;
	}
}
