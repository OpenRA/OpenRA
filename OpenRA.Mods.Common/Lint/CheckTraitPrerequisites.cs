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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckTraitPrerequisites : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors.Where(a => !a.Key.StartsWith("^")))
			{
				try
				{
					var hasTraits = actorInfo.Value.TraitsInConstructOrder().Any();
					if (!hasTraits)
						emitWarning("Actor {0} has no traits. Is this intended?".F(actorInfo.Key));
				}
				catch (Exception e)
				{
					emitError("Actor {0} is not constructible; failure: {1}".F(actorInfo.Key, e.Message));
				}
			}
		}
	}
}