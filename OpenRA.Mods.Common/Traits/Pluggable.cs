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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class PluggableInfo : ITraitInfo, Requires<UpgradeManagerInfo>, UsesInit<PlugsInit>
	{
		[Desc("Footprint cell offset where a plug can be placed.")]
		public readonly CVec Offset = CVec.Zero;

		[FieldLoader.Require, Desc("Upgrades to grant for each accepted plug type.")]
		public readonly Dictionary<string, string[]> Upgrades = null;

		public object Create(ActorInitializer init) { return new Pluggable(init, this); }
	}

	public class Pluggable : INotifyCreated
	{
		public readonly PluggableInfo Info;

		readonly string initialPlug;
		readonly UpgradeManager upgradeManager;

		string active;

		public Pluggable(ActorInitializer init, PluggableInfo info)
		{
			Info = info;
			upgradeManager = init.Self.Trait<UpgradeManager>();

			var plugInit = init.Contains<PlugsInit>() ? init.Get<PlugsInit, Dictionary<CVec, string>>() : new Dictionary<CVec, string>();
			if (plugInit.ContainsKey(Info.Offset))
				initialPlug = plugInit[Info.Offset];
		}

		public void Created(Actor self)
		{
			if (!string.IsNullOrEmpty(initialPlug))
				EnablePlug(self, initialPlug);
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

	public class PlugsInit : IActorInit<Dictionary<CVec, string>>
	{
		[DictionaryFromYamlKey]
		readonly Dictionary<CVec, string> value = new Dictionary<CVec, string>();
		public PlugsInit() { }
		public PlugsInit(Dictionary<CVec, string> init) { value = init; }
		public Dictionary<CVec, string> Value(World world) { return value; }
	}
}
