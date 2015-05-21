#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Mods.D2k.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Warheads
{
	[Desc("Interacts with the DynamicOwnerChange trait.")]
	public class ChangeOwnerWarhead : Warhead
	{
		[Desc("Duration of the owner change (in ticks). Set to 0 to make it permanent.")]
		public readonly int Duration = 0;

		public readonly WRange Range = WRange.FromCells(1);

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var actors = target.Type == TargetType.Actor ? new[] { target.Actor } :
				firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			foreach (var a in actors)
			{
				if (a.Owner == firedBy.Owner) // don't do anything on friendly fire
					continue;

				if (Duration == 0)
					a.ChangeOwner(firedBy.Owner); // permanent
				else
				{
					var tempOwnerManager = a.TraitOrDefault<TemporaryOwnerManager>();
					if (tempOwnerManager == null)
						continue;

					tempOwnerManager.ChangeOwner(a, firedBy.Owner, Duration);
				}

				a.CancelActivity(); // stop shooting, you have got new enemies
			}
		}
	}
}
