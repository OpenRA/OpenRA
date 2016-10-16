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
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckAnimations : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				var attackAnims = actorInfo.Value.TraitInfos<WithAttackAnimationInfo>().ToList();
				var aimAnims = actorInfo.Value.TraitInfos<WithAimAnimationInfo>().ToList();
				if (!attackAnims.Any())
					continue;
				if (!aimAnims.Any())
					continue;

				emitWarning("Actor type {0} defines both WithAttackAnimation and WithAimAnimation, which must not be enabled at the same time!"
					.F(actorInfo.Key));
			}
		}
	}
}
