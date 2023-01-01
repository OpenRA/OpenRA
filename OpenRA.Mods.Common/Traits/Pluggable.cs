#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	public class PluggableInfo : TraitInfo, IEditorActorOptions
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

		[Desc("Options to display in the map editor.",
			"Key is the plug type that the requirements applies to.",
			"Value is the label that is displayed in the actor editor dropdown.")]
		public readonly Dictionary<string, string> EditorOptions = new Dictionary<string, string>();

		[Desc("Label to use for an empty plug socket.")]
		public readonly string EmptyOption = "Empty";

		[Desc("Display order for the dropdown in the map editor")]
		public readonly int EditorDisplayOrder = 5;

		[GrantedConditionReference]
		public IEnumerable<string> LinterConditions => Conditions.Values;

		[ConsumedConditionReference]
		public IEnumerable<string> ConsumedConditions
		{
			get { return Requirements.Values.SelectMany(r => r.Variables).Distinct(); }
		}

		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			if (EditorOptions.Count == 0)
				yield break;

			// Make sure the no-plug option is always available
			EditorOptions[""] = EmptyOption;
			yield return new EditorActorDropdown("Plug", EditorDisplayOrder, EditorOptions,
				actor =>
				{
					var init = actor.GetInitOrDefault<PlugInit>(this);
					return init?.Value ?? "";
				},
				(actor, value) =>
				{
					if (string.IsNullOrEmpty(value))
						actor.RemoveInit<PlugInit>(this);
					else
						actor.ReplaceInit(new PlugInit(this, value), this);
				});
		}

		public override object Create(ActorInitializer init) { return new Pluggable(init, this); }
	}

	public class Pluggable : IObservesVariables, INotifyCreated
	{
		public readonly PluggableInfo Info;

		readonly string initialPlug;
		int conditionToken = Actor.InvalidConditionToken;
		readonly Dictionary<string, bool> plugTypesAvailability = null;

		string active;

		public Pluggable(ActorInitializer init, PluggableInfo info)
		{
			Info = info;

			initialPlug = init.GetValue<PlugInit, string>(info, null);

			if (info.Requirements.Count > 0)
			{
				plugTypesAvailability = new Dictionary<string, bool>();
				foreach (var plug in info.Requirements)
					plugTypesAvailability[plug.Key] = true;
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			if (!string.IsNullOrEmpty(initialPlug))
				EnablePlug(self, initialPlug);
		}

		public bool AcceptsPlug(string type)
		{
			if (!Info.Conditions.ContainsKey(type))
				return false;

			if (!Info.Requirements.ContainsKey(type))
				return active == null;

			return plugTypesAvailability[type];
		}

		public void EnablePlug(Actor self, string type)
		{
			if (!Info.Conditions.TryGetValue(type, out var condition))
				return;

			if (conditionToken != Actor.InvalidConditionToken)
				self.RevokeCondition(conditionToken);

			conditionToken = self.GrantCondition(condition);
			active = type;
		}

		public void DisablePlug(Actor self, string type)
		{
			if (type != active)
				return;

			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			active = null;
		}

		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			foreach (var req in Info.Requirements)
				yield return new VariableObserver(
					(self, variables) => plugTypesAvailability[req.Key] = req.Value.Evaluate(variables),
					req.Value.Variables);
		}
	}

	public class PlugInit : ValueActorInit<string>
	{
		public PlugInit(TraitInfo info, string value)
			: base(info, value) { }
	}
}
