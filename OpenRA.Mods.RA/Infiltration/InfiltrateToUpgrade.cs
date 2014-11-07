#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Infiltration
{
	class InfiltrateToUpgradeInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[Desc("Upgrade provided by infiltration.",
			"Default upgrade types for this and DisableUpgrade match to allow disabling on infiltration without changing types.")]
		public readonly string GrantsUpgrade = "disable";

		[Desc("Duration of the upgrade (in ticks). Set to 0 for a permanent upgrade.")]
		public readonly int Duration = 25 * 30;

		[Desc("Rremove upgrade if captured by actor of given stance with respect to infiltrator")]
		public readonly Stance RemovalCaptureStances = Stance.SameOwner;

		public object Create(ActorInitializer init) { return new InfiltrateToUpgrade(init.self, this); }
	}

	class InfiltrateToUpgrade : INotifyOwnerChanged, INotifyInfiltrated
	{
		readonly InfiltrateToUpgradeInfo info;
		readonly UpgradeManager upgradeManager;
		Player infiltratingPlayer;
		object source;

		public InfiltrateToUpgrade(Actor self, InfiltrateToUpgradeInfo info)
		{
			this.info = info;
			upgradeManager = self.Trait<UpgradeManager>();
		}

		public void Infiltrated(Actor self, Actor infiltrator)
		{
			infiltratingPlayer = infiltrator.Owner;
			source = info.Duration > 0 ?
				upgradeManager.GrantTimedUpgrade(self, info.GrantsUpgrade, info.Duration) :
				upgradeManager.GrantUpgrade(self, info.GrantsUpgrade, this);
		}

		public virtual void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (infiltratingPlayer != null && infiltratingPlayer.Stances[newOwner].AnyFlag(info.RemovalCaptureStances))
				upgradeManager.RevokeUpgrade(self, info.GrantsUpgrade, source);
		}
	}
}