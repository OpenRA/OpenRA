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
using OpenRA.Mods.Common.Warheads;
using OpenRA.Mods.D2k.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Warheads
{
	[Desc("Interacts with the DynamicOwnerChange trait.")]
	public class ChangeOwnerWarhead : Warhead
	{
		[Desc("Duration of the owner change (in ticks). Set to 0 to make it permanent.")]
		public readonly int Duration = 0;

		public readonly WDist Range = WDist.FromCells(1);

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var actors = target.Type == TargetType.Actor ? new[] { target.Actor } :
				firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			foreach (var a in actors)
			{
				// Don't do anything on friendly fire
				if (a.Owner == firedBy.Owner)
					continue;

				if (Duration == 0)
					a.ChangeOwner(firedBy.Owner); // Permanent
				else
				{
					var tempOwnerManager = a.TraitOrDefault<TemporaryOwnerManager>();
					if (tempOwnerManager == null)
						continue;

					tempOwnerManager.ChangeOwner(a, firedBy.Owner, Duration);
				}

				// Stop shooting, you have new enemies
				a.CancelActivity();
			}
		}
	}
}
