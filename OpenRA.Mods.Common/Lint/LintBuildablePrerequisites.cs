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

namespace OpenRA.Mods.Common.Lint
{
	class LintBuildablePrerequisites : ILintRulesPass, ILintServerMapPass
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
			var providedPrereqs = rules.Actors.SelectMany(a => a.Value.TraitInfos<ITechTreePrerequisiteInfo>().SelectMany(p => p.Prerequisites(a.Value)));

			// TODO: this check is case insensitive while the real check in-game is not
			foreach (var actorInfo in rules.Actors)
			{
				// Catch TypeDictionary errors
				try
				{
					var bi = actorInfo.Value.TraitInfoOrDefault<BuildableInfo>();
					if (bi != null)
						foreach (var prereq in bi.Prerequisites)
							if (!prereq.StartsWith("~disabled") && !providedPrereqs.Contains(prereq.Replace("!", "").Replace("~", "")))
								emitError($"Buildable actor {actorInfo.Key} has prereq {prereq} not provided by anything.");
				}
				catch (InvalidOperationException e)
				{
					emitError($"{e.Message} (Actor type `{actorInfo.Key}`)");
				}
			}
		}
	}
}
