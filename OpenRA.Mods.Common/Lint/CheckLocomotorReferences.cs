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

		void Run(Action<string> emitError, Ruleset rules)
		{
			var worldActor = rules.Actors[SystemActors.World];
			var locomotorInfos = worldActor.TraitInfos<LocomotorInfo>().ToArray();
			foreach (var li in locomotorInfos)
				foreach (var otherLocomotor in locomotorInfos)
					if (li != otherLocomotor && li.Name == otherLocomotor.Name)
						emitError($"There is more than one Locomotor with name {li.Name}!");

			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var fields = traitInfo.GetType().GetFields().Where(f => f.HasAttribute<LocomotorReferenceAttribute>());
					foreach (var field in fields)
					{
						var locomotors = LintExts.GetFieldValues(traitInfo, field);
						foreach (var locomotor in locomotors)
						{
							if (string.IsNullOrEmpty(locomotor))
								continue;

							CheckLocomotors(actorInfo.Value, emitError, locomotorInfos, locomotor);
						}
					}
				}
			}
		}

		void CheckLocomotors(ActorInfo actorInfo, Action<string> emitError, LocomotorInfo[] locomotorInfos, string locomotor)
		{
			if (!locomotorInfos.Any(l => l.Name == locomotor))
				emitError($"Actor {actorInfo.Name} defines Locomotor {locomotor} not found on World actor.");
		}
	}
}
