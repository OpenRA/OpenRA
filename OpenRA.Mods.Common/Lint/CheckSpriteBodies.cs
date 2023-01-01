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
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	class CheckSpriteBodies : ILintRulesPass, ILintServerMapPass
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
				var wsbs = actorInfo.Value.TraitInfos<WithSpriteBodyInfo>();
				foreach (var wsb in wsbs)
					if (wsbs.Any(w => w != wsb && w.Name == wsb.Name))
						emitError($"Actor type `{actorInfo.Key}` has more than one *SpriteBody with Name: {wsb.Name}!");
			}
		}
	}
}
