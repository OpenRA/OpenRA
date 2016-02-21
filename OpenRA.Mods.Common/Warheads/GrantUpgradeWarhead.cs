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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class GrantUpgradeWarhead : Warhead
	{
		[UpgradeGrantedReference]
		[Desc("The upgrades to apply.")]
		public readonly string[] Upgrades = { };

		[Desc("Duration of the upgrade (in ticks). Set to 0 for a permanent upgrade.")]
		public readonly int Duration = 0;

		public readonly WDist Range = WDist.FromCells(1);

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var actors = target.Type == TargetType.Actor ? new[] { target.Actor } :
				firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			foreach (var a in actors)
			{
				if (!IsValidAgainst(a, firedBy))
					continue;

				var um = a.TraitOrDefault<UpgradeManager>();
				if (um == null)
					continue;

				foreach (var u in Upgrades)
				{
					if (Duration > 0)
					{
						if (um.AcknowledgesUpgrade(a, u))
							um.GrantTimedUpgrade(a, u, Duration, firedBy, Upgrades.Count(upg => upg == u));
					}
					else
					{
						if (um.AcceptsUpgrade(a, u))
							um.GrantUpgrade(a, u, this);
					}
				}
			}
		}
	}
}
