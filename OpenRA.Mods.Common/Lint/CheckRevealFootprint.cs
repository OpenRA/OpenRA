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
	class CheckRevealFootprint : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				if (actorInfo.Key.StartsWith("^"))
					continue;

				var ios = actorInfo.Value.TraitInfoOrDefault<IOccupySpaceInfo>();
				foreach (var rsi in actorInfo.Value.TraitInfos<RevealsShroudInfo>())
				{
					if (rsi.Type == VisibilityType.CenterPosition)
						continue;

					if (ios == null)
						emitError("Actor type `{0}` defines VisibilityType.Footprint in `{1}` but has no IOccupySpace traits!".F(actorInfo.Key, rsi.GetType()));
					else if (!ios.OccupiedCells(actorInfo.Value, CPos.Zero).Any())
						emitError("Actor type `{0}` defines VisibilityType.Footprint in `{1}`  but does not have any footprint cells!".F(actorInfo.Key, rsi.GetType()));
				}
			}
		}
	}
}
