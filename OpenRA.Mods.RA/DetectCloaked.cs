#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class DetectCloakedInfo : TraitInfo<DetectCloaked>
	{
		public readonly int Interval = 12;		// ~.5s
		public readonly float DecloakTime = 2f;	// 2s
		public readonly int Range = 5;
		public readonly bool AffectOwnUnits = true;
	}

	class DetectCloaked : ITick
	{
		[Sync]
		int ticks;

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var info = self.Info.Traits.Get<DetectCloakedInfo>();
				ticks = info.Interval;

				var toDecloak = self.World.FindUnitsInCircle(self.CenterLocation, info.Range * Game.CellSize)
					.Where(a => a.HasTrait<Cloak>());

				if (!info.AffectOwnUnits)
					toDecloak = toDecloak.Where(a => self.Owner.Stances[a.Owner] != Stance.Ally);

				foreach (var a in toDecloak)
					a.Trait<Cloak>().Decloak((int)(25 * info.DecloakTime));
			}
		}
	}
}
