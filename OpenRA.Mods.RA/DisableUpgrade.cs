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
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class DisableUpgradeInfo : ITraitInfo
	{
		public readonly string RequiresUpgrade = "disable";

		public object Create(ActorInitializer init) { return new DisableUpgrade(this); }
	}

	public class DisableUpgrade : IUpgradable, IDisable
	{
		readonly DisableUpgradeInfo info;
		bool enabled;

		public DisableUpgrade(DisableUpgradeInfo info)
		{
			this.info = info;
		}

		public bool AcceptsUpgrade(string type)
		{
			return type == info.RequiresUpgrade;
		}

		public void UpgradeAvailable(Actor self, string type, bool available)
		{
			if (type == info.RequiresUpgrade)
				enabled = available;
		}

		public bool Disabled { get { return enabled; } }
	}
}
