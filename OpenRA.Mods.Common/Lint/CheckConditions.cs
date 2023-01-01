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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckConditions : ILintRulesPass, ILintServerMapPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			Run(emitError, emitWarning, rules);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			Run(emitError, emitWarning, mapRules);
		}

		void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				var granted = new HashSet<string>();
				var consumed = new HashSet<string>();

				foreach (var trait in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var fieldConsumed = trait.GetType().GetFields()
						.Where(x => x.HasAttribute<ConsumedConditionReferenceAttribute>())
						.SelectMany(f => LintExts.GetFieldValues(trait, f));

					var propertyConsumed = trait.GetType().GetProperties()
						.Where(x => x.HasAttribute<ConsumedConditionReferenceAttribute>())
						.SelectMany(p => LintExts.GetPropertyValues(trait, p));

					var fieldGranted = trait.GetType().GetFields()
						.Where(x => x.HasAttribute<GrantedConditionReferenceAttribute>())
						.SelectMany(f => LintExts.GetFieldValues(trait, f));

					var propertyGranted = trait.GetType().GetProperties()
						.Where(x => x.HasAttribute<GrantedConditionReferenceAttribute>())
						.SelectMany(f => LintExts.GetPropertyValues(trait, f));

					foreach (var c in fieldConsumed.Concat(propertyConsumed))
						if (!string.IsNullOrEmpty(c))
							consumed.Add(c);

					foreach (var g in fieldGranted.Concat(propertyGranted))
						if (!string.IsNullOrEmpty(g))
							granted.Add(g);
				}

				var unconsumed = granted.Except(consumed);
				if (unconsumed.Any())
					emitWarning($"Actor type `{actorInfo.Key}` grants conditions that are not consumed: {unconsumed.JoinWith(", ")}");

				var ungranted = consumed.Except(granted);
				if (ungranted.Any())
					emitError($"Actor type `{actorInfo.Key}` consumes conditions that are not granted: {ungranted.JoinWith(", ")}");
			}
		}
	}
}
