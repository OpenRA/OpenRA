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

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Trigger upgrades when an actor is targeted.")]
	public class UpgradeOnTargetInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[UpgradeGrantedReference] public readonly string[] Upgrades = { };

		public object Create(ActorInitializer init) { return new UpgradeOnTarget(init, this); }
	}

	public class UpgradeOnTarget : ITick
	{
		readonly Actor self;
		readonly UpgradeOnTargetInfo info;
		readonly UpgradeManager manager;

		bool granted;

		public UpgradeOnTarget(ActorInitializer init, UpgradeOnTargetInfo info)
		{
			self = init.Self;
			this.info = info;
			manager = self.Trait<UpgradeManager>();
		}

		public void Tick(Actor self)
		{
			var activity = self.GetCurrentActivity();
			var wantsGranted = activity != null && activity.GetTargets(self).Any(t => t.Type == TargetType.Actor);
			if (wantsGranted && !granted)
			{
				foreach (var up in info.Upgrades)
					manager.GrantUpgrade(self, up, this);

				granted = true;
			}
			else if (!wantsGranted && granted)
			{
				foreach (var up in info.Upgrades)
					manager.RevokeUpgrade(self, up, this);

				granted = false;
			}
		}
	}
}
