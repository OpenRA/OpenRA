#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckLocomotorReferences : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			var worldActor = rules.Actors["world"];
			var locomotorInfos = worldActor.TraitInfos<LocomotorInfo>().ToArray();
			foreach (var li in locomotorInfos)
				foreach (var otherLocomotor in locomotorInfos)
					if (li != otherLocomotor && li.Name == otherLocomotor.Name)
						emitError("There is more than one Locomotor with name {0}!".F(li.Name));

			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var fields = traitInfo.GetType().GetFields().Where(f => f.HasAttribute<LocomotorReferenceAttribute>());
					foreach (var field in fields)
					{
						var locomotors = LintExts.GetFieldValues(traitInfo, field, emitError);
						foreach (var locomotor in locomotors)
						{
							if (string.IsNullOrEmpty(locomotor))
								continue;

							CheckLocomotors(actorInfo.Value, emitError, rules, locomotorInfos, locomotor);
						}
					}
				}
			}
		}

		void CheckLocomotors(ActorInfo actorInfo, Action<string> emitError, Ruleset rules, LocomotorInfo[] locomotorInfos, string locomotor)
		{
			if (!locomotorInfos.Any(l => l.Name == locomotor))
				emitError("Actor {0} defines Locomotor {1} not found on World actor.".F(actorInfo.Name, locomotor));
		}
	}
}
