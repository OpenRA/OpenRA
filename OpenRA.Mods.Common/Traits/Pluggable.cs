#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class PluggableInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[Desc("Footprint cell offset where a plug can be placed.")]
		public readonly CVec Offset = CVec.Zero;

		[FieldLoader.LoadUsing("LoadUpgrades", true)]
		[Desc("Upgrades to grant for each accepted plug type.")]
		public readonly Dictionary<string, string[]> Upgrades = null;

		static object LoadUpgrades(MiniYaml y)
		{
			return y.ToDictionary()["Upgrades"].Nodes.ToDictionary(
				kv => kv.Key,
				kv => FieldLoader.GetValue<string[]>("(value)", kv.Value.Value));
		}

		public object Create(ActorInitializer init) { return new Pluggable(init.Self, this); }
	}

	public class Pluggable
	{
		public readonly PluggableInfo Info;
		readonly UpgradeManager upgradeManager;
		string active;

		public Pluggable(Actor self, PluggableInfo info)
		{
			Info = info;
			upgradeManager = self.Trait<UpgradeManager>();
		}

		public bool AcceptsPlug(Actor self, string type)
		{
			return active == null && Info.Upgrades.ContainsKey(type);
		}

		public void EnablePlug(Actor self, string type)
		{
			string[] upgrades;
			if (!Info.Upgrades.TryGetValue(type, out upgrades))
				return;

			foreach (var u in upgrades)
				upgradeManager.GrantUpgrade(self, u, this);

			active = type;
		}

		public void DisablePlug(Actor self, string type)
		{
			if (type != active)
				return;

			foreach (var u in Info.Upgrades[type])
				upgradeManager.RevokeUpgrade(self, u, this);
		}
	}
}
