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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class GlobalUpgradableInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		public readonly string[] Upgrades = { };
		public readonly string[] Prerequisites = { };

		public object Create(ActorInitializer init) { return new GlobalUpgradable(init.self, this); }
	}

	public class GlobalUpgradable : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly GlobalUpgradableInfo info;
		readonly GlobalUpgradeManager globalManager;
		readonly UpgradeManager manager;
		bool wasAvailable;

		public GlobalUpgradable(Actor self, GlobalUpgradableInfo info)
		{
			this.info = info;
			globalManager = self.Owner.PlayerActor.Trait<GlobalUpgradeManager>();
			manager = self.Trait<UpgradeManager>();
		}

		public void AddedToWorld(Actor self)
		{
			if (info.Prerequisites.Any())
				globalManager.Register(self, this, info.Prerequisites);
		}

		public void RemovedFromWorld(Actor self)
		{
			if (info.Prerequisites.Any())
				globalManager.Unregister(self, this, info.Prerequisites);
		}

		public void PrerequisitesUpdated(Actor self, bool available)
		{
			if (available == wasAvailable)
				return;
			
			if (available)
				foreach (var u in info.Upgrades)
					manager.GrantUpgrade(self, u, this);
			else
				foreach (var u in info.Upgrades)
					manager.RevokeUpgrade(self, u, this);

			wasAvailable = available;
		}
	}
}
