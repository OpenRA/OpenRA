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
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckTraitPrerequisites : ILintRulesPass, ILintServerMapPass
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
				try
				{
					var hasTraits = actorInfo.Value.TraitsInConstructOrder().Any();
					if (!hasTraits)
						emitWarning($"Actor {actorInfo.Key} has no traits. Is this intended?");
				}
				catch (Exception e)
				{
					emitError($"Actor {actorInfo.Key} is not constructible; failure: {e.Message}");
				}
			}
		}
	}
}
