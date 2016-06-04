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
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckRangeLimit : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var weaponInfo in rules.Weapons)
			{
				var range = weaponInfo.Value.Range;
				var missile = weaponInfo.Value.Projectile as MissileInfo;

				if (missile != null && missile.RangeLimit > WDist.Zero && missile.RangeLimit < range)
					emitError("Weapon `{0}`: projectile RangeLimit lower than weapon range!"
						.F(weaponInfo.Key));
			}
		}
	}
}
