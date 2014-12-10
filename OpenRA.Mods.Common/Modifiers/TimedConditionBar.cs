#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("Visualizes the remaining time for a condition's timer.")]
	class TimedConditionBarInfo : ITraitInfo, Requires<ConditionManagerInfo>
	{
		[Desc("Condition to which this bar corresponds.")]
		public readonly string Condition = null;

		public readonly Color Color = Color.Red;

		public object Create(ActorInitializer init) { return new TimedConditionBar(init.self, this); }
	}

	class TimedConditionBar : ISelectionBar
	{
		readonly TimedConditionBarInfo info;
		readonly Actor self;
		float value;

		public TimedConditionBar(Actor self, TimedConditionBarInfo info)
		{
			this.self = self;
			this.info = info;

			self.Trait<ConditionManager>().RegisterWatcher(info.Condition, Update);
		}

		public void Update(int duration, int remaining)
		{
			value = remaining * 1f / duration;
		}

		public float GetValue()
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			return value;
		}

		public Color GetColor() { return info.Color; }
	}
}
