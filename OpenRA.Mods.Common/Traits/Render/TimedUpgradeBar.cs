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

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Visualizes the remaining time for an upgrade.")]
	class TimedUpgradeBarInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[FieldLoader.Require]
		[Desc("Upgrade that this bar corresponds to")]
		public readonly string Upgrade = null;

		public readonly Color Color = Color.Red;

		public object Create(ActorInitializer init) { return new TimedUpgradeBar(init.Self, this); }
	}

	class TimedUpgradeBar : ISelectionBar, INotifyCreated
	{
		readonly TimedUpgradeBarInfo info;
		readonly Actor self;
		float value;

		public TimedUpgradeBar(Actor self, TimedUpgradeBarInfo info)
		{
			this.self = self;
			this.info = info;
		}

		public void Created(Actor self)
		{
			self.Trait<UpgradeManager>().RegisterWatcher(info.Upgrade, Update);
		}

		public void Update(int duration, int remaining)
		{
			value = remaining * 1f / duration;
		}

		float ISelectionBar.GetValue()
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			return value;
		}

		Color ISelectionBar.GetColor() { return info.Color; }
	}
}
