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
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class GlobalUpgradableInfo : ITraitInfo
	{
		public readonly string[] Upgrades = { };
		public readonly string[] Prerequisites = { };

		public object Create(ActorInitializer init) { return new GlobalUpgradable(init.self, this); }
	}

	public class GlobalUpgradable : INotifyAddedToWorld, INotifyRemovedFromWorld, ITechTreeElement
	{
		readonly GlobalUpgradableInfo info;
		readonly TechTree tt;
		readonly Actor self;

		public GlobalUpgradable(Actor actor, GlobalUpgradableInfo info)
		{
			this.info = info;
			tt = actor.Owner.PlayerActor.Trait<TechTree>();
			self = actor;
		}

		public void AddedToWorld(Actor self)
		{
			if (info.Prerequisites.Any())
				tt.Add(info.Prerequisites, this);
		}

		public void RemovedFromWorld(Actor self)
		{
			if (info.Prerequisites.Any())
				tt.Remove(this);
		}

		public void PrerequisitesUpdated(bool available)
		{
			var upgrades = self.TraitsImplementing<IUpgradable>();
			foreach (var u in upgrades)
			{
				foreach (var t in info.Upgrades)
					if (u.AcceptsUpgrade(t))
						u.UpgradeAvailable(self, t, available);
			}
		}

		public void PrerequisitesAvailable() { PrerequisitesUpdated(true); }
		public void PrerequisitesUnavailable() { PrerequisitesUpdated(false); }
		public void PrerequisitesItemHidden() { }
		public void PrerequisitesItemVisible() { }
	}
}
