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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckConditions : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				var withGranted = actorInfo.Value.TraitInfos<TraitInfo>()
					.SelectMany(trait => Exts.EnumerateInFieldsAndProperties<IWithGrantedVariables>(trait));

				var withUsed = actorInfo.Value.TraitInfos<TraitInfo>()
					.SelectMany(trait => Exts.EnumerateInFieldsAndProperties<IWithUsedVariables>(trait));

				var allGranted = new Dictionary<string, HashSet<Type>>();
				foreach (var granted in withGranted.GetGrantedVariables())
					allGranted.GetOrAdd(granted.Key).Add(granted.Value);

				var allConsumed = new HashSet<string>(withUsed.GetUsedVariables());

				var unconsumed = allGranted.Keys.Except(allConsumed);
				if (unconsumed.Any())
					emitWarning("Actor type `{0}` grants conditions that are not consumed: {1}".F(actorInfo.Key, unconsumed.JoinWith(", ")));

				var ungranted = allConsumed.Except(allGranted.Keys);
				if (ungranted.Any())
					emitError("Actor type `{0}` consumes conditions that are not granted: {1}".F(actorInfo.Key, ungranted.JoinWith(", ")));
			}
		}
	}
}
