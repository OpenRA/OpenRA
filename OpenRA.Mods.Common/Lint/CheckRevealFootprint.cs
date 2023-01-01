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
using OpenRA.Mods.Common.Traits;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckRevealFootprint : ILintRulesPass, ILintServerMapPass
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
			foreach (var actorInfo in rules.Actors)
			{
				// Catch TypeDictionary errors
				try
				{
					var ios = actorInfo.Value.TraitInfoOrDefault<IOccupySpaceInfo>();
					foreach (var rsi in actorInfo.Value.TraitInfos<RevealsShroudInfo>())
					{
						if (rsi.Type != VisibilityType.Footprint)
							continue;

						if (ios == null)
							emitError($"Actor type `{actorInfo.Key}` defines VisibilityType.Footprint in `{rsi.GetType()}` but has no IOccupySpace traits!");
						else if (ios.OccupiedCells(actorInfo.Value, CPos.Zero).Count == 0)
							emitError($"Actor type `{actorInfo.Key}` defines VisibilityType.Footprint in `{rsi.GetType()}` but does not have any footprint cells!");
					}
				}
				catch (InvalidOperationException e)
				{
					emitError($"{e.Message} (Actor type `{actorInfo.Key}`)");
				}
			}
		}
	}
}
