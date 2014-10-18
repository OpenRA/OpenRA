#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Infiltration
{
	class InfiltrateToUpgradeInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[Desc("Upgrade provided by infiltration.",
			"Default upgrade types for this and DisableUpgrade match to allow disabling on infiltration without changing types.")]
		public readonly string ProvidesUpgrade = "disable";

		[Desc("Duration of upgrade in ticks. 0 - no duration limit")]
		public readonly int Duration = 25 * 30;

		[Desc("Whether to remove upgrade if captured by same owner as infiltration", "Duration must be 0.")]
		public readonly bool RemoveWhenCapturedByOwner = true;

		public object Create(ActorInitializer init) { return new InfiltrateToUpgrade(init.self, this); }
	}

	class InfiltrateToUpgrade : INotifyOwnerChanged, INotifyInfiltrated
	{
		readonly InfiltrateToUpgradeInfo info;
		readonly UpgradeManager upgradeManager;
		Player infiltratingPlayer;

		public InfiltrateToUpgrade(Actor self, InfiltrateToUpgradeInfo info)
		{
			this.info = info;
			upgradeManager = self.Trait<UpgradeManager>();
		}

		public void Infiltrated(Actor self, Actor infiltrator)
		{
			infiltratingPlayer = infiltrator.Owner;
			if (info.Duration > 0)
				upgradeManager.GrantTimedUpgrade(self, info.ProvidesUpgrade, info.Duration);
			else
				upgradeManager.GrantUpgrade(self, info.ProvidesUpgrade, this);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (newOwner == infiltratingPlayer && info.RemoveWhenCapturedByOwner && info.Duration == 0)
				upgradeManager.RevokeUpgrade(self, info.ProvidesUpgrade, this);
		}
	}
}