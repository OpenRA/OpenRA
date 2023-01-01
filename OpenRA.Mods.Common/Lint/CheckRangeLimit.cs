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
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	class CheckRangeLimit : ILintRulesPass, ILintServerMapPass
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
			foreach (var weaponInfo in rules.Weapons)
			{
				var range = weaponInfo.Value.Range;

				if (weaponInfo.Value.Projectile is MissileInfo missile && missile.RangeLimit > WDist.Zero && missile.RangeLimit < range)
					emitError($"Weapon `{weaponInfo.Key}`: projectile RangeLimit lower than weapon range!");
			}
		}
	}
}
