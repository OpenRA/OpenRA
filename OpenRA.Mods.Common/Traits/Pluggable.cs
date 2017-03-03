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

using System;
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
		[Desc("Conditions to grant for each accepted plug type.")]
		public readonly Dictionary<string, string> Conditions = null;

		[ProvidedConditionReference]
		[Desc("Name of condition for checking plug.",
			"(this-name).(condition-name) expression returns a boolean for whether this plug is granting a specified condition.")]
		public readonly string Condition = null;

		[GrantedConditionReference]
		public IEnumerable<string> LinterConditions { get { return Conditions.Values; } }

		[ConsumedConditionReference]
		public IEnumerable<string> LinterSelfConditions
		{
			get { return string.IsNullOrEmpty(Condition) ? Enumerable.Empty<string>() : Conditions.Values; }
		}

		public object Create(ActorInitializer init) { return new Pluggable(init, this); }
	}

	public class Pluggable : NotifyingCondition, INotifyCreated, INotifyingConditionProvider
	{
		public readonly PluggableInfo Info;

		readonly string initialPlug;
		int conditionToken = ConditionManager.InvalidConditionToken;

		string active;
		string condition;

		IEnumerable<KeyValuePair<string, INotifyingCondition>> INotifyingConditionProvider.Provided
		{
			get
			{
				if (!string.IsNullOrEmpty(Info.Condition))
					yield return new KeyValuePair<string, INotifyingCondition>(Info.Condition, this);
			}
		}

		public Pluggable(ActorInitializer init, PluggableInfo info)
		{
			Info = info;

			var plugInit = init.Contains<PlugsInit>() ? init.Get<PlugsInit, Dictionary<CVec, string>>() : new Dictionary<CVec, string>();
			if (plugInit.ContainsKey(Info.Offset))
				initialPlug = plugInit[Info.Offset];
		}

		public void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();

			if (!string.IsNullOrEmpty(initialPlug))
				EnablePlug(self, initialPlug);
		}

		public bool AcceptsPlug(Actor self, string type)
		{
			return active == null && Info.Conditions.ContainsKey(type);
		}

		public void EnablePlug(Actor self, string type)
		{
			string newCondition;
			if (!Info.Conditions.TryGetValue(type, out newCondition))
				return;

			condition = newCondition;
			conditionToken = conditionManager.GrantCondition(self, condition);
			active = type;
			condition = newCondition;
			NotifyConditionChanged(self);
		}

		public void DisablePlug(Actor self, string type)
		{
			if (type != active)
				return;

			if (conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);

			active = null;
			condition = null;
			NotifyConditionChanged(self);
		}

		public override bool AsBool() { return condition != null; }
		public override int AsInt() { return condition != null ? 1 : 0; }
		public override ICondition Get(string name)
		{
			return condition == name ? BoolCondition.True : BoolCondition.False;
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
