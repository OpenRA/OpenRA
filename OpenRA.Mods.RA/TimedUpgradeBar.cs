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

namespace OpenRA.Mods.RA
{
	[Desc("Visualizes the remaining time for an upgrade.")]
	class TimedUpgradeBarInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[Desc("Corresponding upgrade to this bar.")]
		public readonly string Upgrade = null;

		[Desc("Color of the bar.")]
		public readonly Color Color = Color.Red;

		[Desc("Player stances for which the bar should be shown.")]
		public readonly Stance Visibility = Stance.SameOwner;

		public object Create(ActorInitializer init) { return new TimedUpgradeBar(init.self, this); }
	}

	class TimedUpgradeBar : ISelectionBar
	{
		readonly TimedUpgradeBarInfo info;
		readonly Actor self;
		float value;

		public TimedUpgradeBar(Actor self, TimedUpgradeBarInfo info)
		{
			this.self = self;
			this.info = info;

			self.Trait<UpgradeManager>().RegisterWatcher(info.Upgrade, Update);
		}

		public void Update(int duration, int remaining)
		{
			value = remaining * 1f / duration;
		}

		public float GetValue()
		{
			return self.Owner.Stances[self.World.RenderPlayer].AnyFlag(info.Visibility) ? value : 0;
		}

		public Color GetColor() { return info.Color; }
	}
}
