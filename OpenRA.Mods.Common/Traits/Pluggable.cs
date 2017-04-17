#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class PluggableInfo : ITraitInfo, UsesInit<PlugsInit>
	{
		[Desc("Footprint cell offset where a plug can be placed.")]
		public readonly CVec Offset = CVec.Zero;

		[FieldLoader.Require]
		[Desc("Conditions to grant for each accepted plug type.",
			"Key is the plug type.",
			"Value is the condition that is granted when the plug is enabled.")]
		public readonly Dictionary<string, string> Conditions = null;

		[Desc("Requirements for accepting a plug type.",
			"Key is the plug type that the requirements applies to.",
			"Value is the condition expression defining the requirements to place the plug.")]
		public readonly Dictionary<string, BooleanExpression> Requirements = new Dictionary<string, BooleanExpression>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterConditions { get { return Conditions.Values; } }

		[ConsumedConditionReference]
		public IEnumerable<string> ConsumedConditions
		{
			get { return Requirements.Values.SelectMany(r => r.Variables).Distinct(); }
		}

		public object Create(ActorInitializer init) { return new Pluggable(init, this); }
	}

	public class Pluggable : IConditionConsumer, INotifyCreated
	{
		public readonly PluggableInfo Info;

		readonly string initialPlug;
		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;
		Dictionary<string, bool> plugTypesAvailability = null;

		string active;

		public Pluggable(ActorInitializer init, PluggableInfo info)
		{
			Info = info;

			var plugInit = init.Contains<PlugsInit>() ? init.Get<PlugsInit, Dictionary<CVec, string>>() : new Dictionary<CVec, string>();
			if (plugInit.ContainsKey(Info.Offset))
				initialPlug = plugInit[Info.Offset];

			if (info.Requirements.Count > 0)
			{
				plugTypesAvailability = new Dictionary<string, bool>();
				foreach (var plug in info.Requirements)
					plugTypesAvailability[plug.Key] = true;
			}
		}

		public void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();

			if (!string.IsNullOrEmpty(initialPlug))
				EnablePlug(self, initialPlug);
		}

		public bool AcceptsPlug(Actor self, string type)
		{
			if (!Info.Conditions.ContainsKey(type))
				return false;

			if (!Info.Requirements.ContainsKey(type))
				return active == null;

			return plugTypesAvailability[type];
		}

		public void EnablePlug(Actor self, string type)
		{
			string condition;
			if (!Info.Conditions.TryGetValue(type, out condition))
				return;

			if (conditionToken != ConditionManager.InvalidConditionToken)
				conditionManager.RevokeCondition(self, conditionToken);

			conditionToken = conditionManager.GrantCondition(self, condition);
			active = type;
		}

		public void DisablePlug(Actor self, string type)
		{
			if (type != active)
				return;

			if (conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);

			active = null;
		}

		IEnumerable<string> IConditionConsumer.Conditions { get { return Info.ConsumedConditions; } }

		void IConditionConsumer.ConditionsChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			foreach (var req in Info.Requirements)
				plugTypesAvailability[req.Key] = req.Value.Evaluate(conditions);
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
