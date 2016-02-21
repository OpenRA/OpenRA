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

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class LintBuildablePrerequisites : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			// ProvidesPrerequisite allows arbitrary prereq definitions
			var customPrereqs = rules.Actors.SelectMany(a => a.Value.TraitInfos<ProvidesPrerequisiteInfo>()
				.Select(p => p.Prerequisite ?? a.Value.Name));

			// ProvidesTechPrerequisite allows arbitrary prereq definitions
			// (but only one group at a time during gameplay)
			var techPrereqs = rules.Actors.SelectMany(a => a.Value.TraitInfos<ProvidesTechPrerequisiteInfo>())
				.SelectMany(p => p.Prerequisites);

			var providedPrereqs = customPrereqs.Concat(techPrereqs);

			// TODO: this check is case insensitive while the real check in-game is not
			foreach (var i in rules.Actors)
			{
				var bi = i.Value.TraitInfoOrDefault<BuildableInfo>();
				if (bi != null)
					foreach (var prereq in bi.Prerequisites)
						if (!prereq.StartsWith("~disabled"))
							if (!providedPrereqs.Contains(prereq.Replace("!", "").Replace("~", "")))
								emitError("Buildable actor {0} has prereq {1} not provided by anything.".F(i.Key, prereq));
			}
		}
	}
}