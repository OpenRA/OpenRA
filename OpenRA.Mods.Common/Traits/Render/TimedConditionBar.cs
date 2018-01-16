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

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Visualizes the remaining time for a condition.")]
	class TimedConditionBarInfo : ITraitInfo, Requires<ConditionManagerInfo>
	{
		[FieldLoader.Require]
		[Desc("Condition that this bar corresponds to")]
		public readonly string Condition = null;

		public readonly Color Color = Color.Red;

		public object Create(ActorInitializer init) { return new TimedConditionBar(init.Self, this); }
	}

	class TimedConditionBar : ISelectionBar, IConditionTimerWatcher
	{
		readonly TimedConditionBarInfo info;
		readonly Actor self;
		float value;

		public TimedConditionBar(Actor self, TimedConditionBarInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void IConditionTimerWatcher.Update(int duration, int remaining)
		{
			value = duration > 0 ? remaining * 1f / duration : 0;
		}

		string IConditionTimerWatcher.Condition { get { return info.Condition; } }

		float ISelectionBar.GetValue()
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			return value;
		}

		Color ISelectionBar.GetColor() { return info.Color; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
