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
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Effects;

namespace OpenRA.Mods.RA
{
	class KillsSelfInfo : ITraitInfo
	{
		[Desc("Enable only if this upgrade is enabled.")]
		public readonly string RequiresUpgrade = null;

		[Desc("Remove the actor from the world (and destroy it) instead of killing it.")]
		public readonly bool RemoveInstead = false;

		public object Create(ActorInitializer init) { return new KillsSelf(init.self, this); }
	}

	class KillsSelf : INotifyAddedToWorld, IUpgradable
	{
		readonly KillsSelfInfo info;
		readonly Actor self;

		public KillsSelf(Actor self, KillsSelfInfo info)
		{
			this.info = info;
			this.self = self;
		}

		public void AddedToWorld(Actor self)
		{
			if (info.RequiresUpgrade == null)
				Kill();
		}

		public bool AcceptsUpgrade(string type)
		{
			return type == info.RequiresUpgrade;
		}

		public void UpgradeAvailable(Actor self, string type, bool available)
		{
			if (type == info.RequiresUpgrade)
				Kill();
		}

		void Kill()
		{
			if (self.Flagged(ActorFlag.Dead))
				return;

			if (info.RemoveInstead || !self.HasTrait<Health>())
				self.Destroy();
			else
				self.Kill(self);
		}
	}
}
