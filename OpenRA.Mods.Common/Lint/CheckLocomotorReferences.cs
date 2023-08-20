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
using OpenRA.Mods.Common.Traits;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckLocomotorReferences : ILintRulesPass, ILintServerMapPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			Run(emitError, rules);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			Run(emitError, mapRules);
		}

		static void Run(Action<string> emitError, Ruleset rules)
		{
			var worldActor = rules.Actors[SystemActors.World];
			var locomotorNames = worldActor.TraitInfos<LocomotorInfo>().Select(li => li.Name).ToList();
			var duplicateNames = locomotorNames
				.GroupBy(name => name)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key);
			foreach (var duplicateName in duplicateNames)
				emitError($"More than one Locomotor exists with the name `{duplicateName}`.");

			var locomotorNamesSet = locomotorNames.ToHashSet();
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var fields = Utility.GetFields(traitInfo.GetType()).Where(f => Utility.HasAttribute<LocomotorReferenceAttribute>(f));
					foreach (var field in fields)
					{
						var locomotors = LintExts.GetFieldValues(traitInfo, field);
						foreach (var locomotor in locomotors)
						{
							if (string.IsNullOrEmpty(locomotor))
								continue;

							CheckLocomotors(actorInfo.Value, emitError, locomotorNamesSet, locomotor);
						}
					}
				}
			}
		}

		static void CheckLocomotors(ActorInfo actorInfo, Action<string> emitError, HashSet<string> locomotorNames, string locomotor)
		{
			if (!locomotorNames.Contains(locomotor))
				emitError($"Actor `{actorInfo.Name}` defines Locomotor `{locomotor}` not found on World actor.");
		}
	}
}
