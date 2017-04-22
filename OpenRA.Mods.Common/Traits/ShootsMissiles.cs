#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor fires weapons that shoots missiles.")]
	public class ShootsMissilesInfo : ITraitInfo, Requires<ArmamentInfo>
	{
		[Desc("Weapon used to shoot the missile. Caution: make sure that this is an insta-hit weapon, otherwise will look very odd!")]
		public readonly string Armament = "pdlaser";

		[Desc("What diplomatic stances are affected.")]
		public readonly Stance ShootStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

		public object Create(ActorInitializer init) { return new ShootsMissiles(init.Self, this); }
	}

	public class ShootsMissiles
	{
		readonly ShootsMissilesInfo info;
		readonly Armament armament;

		public Armament Armament { get { return armament; } }
		public WDist Range { get { return armament.MaxRange(); } }

		public Stance DeflectionStances { get { return info.ShootStances; } }

		public ShootsMissiles(Actor self, ShootsMissilesInfo info)
		{
			this.info = info;
			armament = self.TraitsImplementing<Armament>().First(a => a.Info.Name == info.Armament);
		}
	}
}
