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
	public class PluggableInfo : ITraitInfo, UsesInit<PlugsInit>
	{
		[Desc("Footprint cell offset where a plug can be placed.")]
		public readonly CVec Offset = CVec.Zero;

		[FieldLoader.Require]
		[Desc("Conditions to grant for each accepted plug type.")]
		public readonly Dictionary<string, string> Conditions = null;

		[GrantedConditionReference]
		public IEnumerable<string> LinterConditions { get { return Conditions.Values; } }

		public object Create(ActorInitializer init) { return new Pluggable(init, this); }
	}

	public class Pluggable : INotifyCreated
	{
		public readonly PluggableInfo Info;

		readonly string initialPlug;
		UpgradeManager upgradeManager;
		int conditionToken = UpgradeManager.InvalidConditionToken;

		string active;

		public Pluggable(ActorInitializer init, PluggableInfo info)
		{
			Info = info;

			var plugInit = init.Contains<PlugsInit>() ? init.Get<PlugsInit, Dictionary<CVec, string>>() : new Dictionary<CVec, string>();
			if (plugInit.ContainsKey(Info.Offset))
				initialPlug = plugInit[Info.Offset];
		}

		public void Created(Actor self)
		{
			upgradeManager = self.TraitOrDefault<UpgradeManager>();

			if (!string.IsNullOrEmpty(initialPlug))
				EnablePlug(self, initialPlug);
		}

		public bool AcceptsPlug(Actor self, string type)
		{
			return active == null && Info.Conditions.ContainsKey(type);
		}

		public void EnablePlug(Actor self, string type)
		{
			string condition;
			if (!Info.Conditions.TryGetValue(type, out condition))
				return;

			conditionToken = upgradeManager.GrantCondition(self, condition);
			active = type;
		}

		public void DisablePlug(Actor self, string type)
		{
			if (type != active)
				return;

			if (conditionToken != UpgradeManager.InvalidConditionToken)
				conditionToken = upgradeManager.RevokeCondition(self, conditionToken);

			active = null;
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
