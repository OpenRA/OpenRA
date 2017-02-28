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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckConditions : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				if (actorInfo.Key.StartsWith("^", StringComparison.Ordinal))
					continue;

				var granted = new HashSet<string>();
				var provided = new HashSet<string>();
				var consumed = new HashSet<string>();
				var given = new HashSet<string>();
				var multipleProviders = new HashSet<string>();

				foreach (var trait in actorInfo.Value.TraitInfos<ITraitInfo>())
				{
					var fieldConsumed = trait.GetType().GetFields()
						.Where(x => x.HasAttribute<ConsumedConditionReferenceAttribute>())
						.SelectMany(f => LintExts.GetFieldValues(trait, f, emitError));

					var propertyConsumed = trait.GetType().GetProperties()
						.Where(x => x.HasAttribute<ConsumedConditionReferenceAttribute>())
						.SelectMany(p => LintExts.GetPropertyValues(trait, p, emitError));

					var fieldGranted = trait.GetType().GetFields()
                        .Where(x => x.HasAttribute<GrantedConditionReferenceAttribute>())
	  					.SelectMany(f => LintExts.GetFieldValues(trait, f, emitError));

					var propertyGranted = trait.GetType().GetProperties()
                        .Where(x => x.HasAttribute<GrantedConditionReferenceAttribute>())
	  					.SelectMany(f => LintExts.GetPropertyValues(trait, f, emitError));

					var fieldProvided = trait.GetType().GetFields()
                        .Where(x => x.HasAttribute<ProvidedConditionReferenceAttribute>())
                        .SelectMany(f => LintExts.GetFieldValues(trait, f, emitError));

					var propertyProvided = trait.GetType().GetProperties()
                        .Where(x => x.HasAttribute<ProvidedConditionReferenceAttribute>())
	  					.SelectMany(f => LintExts.GetPropertyValues(trait, f, emitError));

					foreach (var c in fieldConsumed.Concat(propertyConsumed))
						if (!string.IsNullOrEmpty(c))
							consumed.Add(c);

					foreach (var g in fieldGranted.Concat(propertyGranted))
						if (!string.IsNullOrEmpty(g))
						{
							granted.Add(g);
							given.Add(g);
						}

					foreach (var g in fieldProvided.Concat(propertyProvided))
						if (!string.IsNullOrEmpty(g))
						{
							if (provided.Contains(g))
								multipleProviders.Add(g);
							provided.Add(g);
							given.Add(g);
						}
				}

				var onlyGranted = granted.Except(consumed);
				if (onlyGranted.Any())
					emitWarning("Actor type `{0}` grants conditions that are not consumed: {1}".F(actorInfo.Key, onlyGranted.JoinWith(", ")));

				var onlyProvided = granted.Except(consumed);
				if (onlyProvided.Any())
					emitWarning("Actor type `{0}` provides condition variables that are not consumed: {1}".F(actorInfo.Key, onlyProvided.JoinWith(", ")));

				var ungiven = consumed.Except(given);
				if (ungiven.Any())
					emitError("Actor type `{0}` consumes conditions that are neither granted nor provided: {1}".F(actorInfo.Key, ungiven.JoinWith(", ")));

				if (multipleProviders.Any())
					emitError("Actor type `{0}` has multiple traits providing these condition variables: {1}".F(actorInfo.Key, multipleProviders.JoinWith(", ")));

				if ((consumed.Any() || given.Any()) && actorInfo.Value.TraitInfoOrDefault<ConditionManagerInfo>() == null)
					emitError("Actor type `{0}` defines conditions but does not include a ConditionManager".F(actorInfo.Key));
			}
		}
	}
}