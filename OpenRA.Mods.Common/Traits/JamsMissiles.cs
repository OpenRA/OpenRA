#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor deflects missiles.")]
	public class JamsMissilesInfo : ConditionalTraitInfo
	{
		[Desc("Range of the deflection.")]
		public readonly WDist Range = WDist.Zero;

		[Desc("What diplomatic stances are affected.")]
		public readonly Stance DeflectionStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

		[Desc("Chance of deflecting missiles.")]
		public readonly int Chance = 100;

		public override object Create(ActorInitializer init) { return new JamsMissiles(this); }
	}

	public class JamsMissiles : ConditionalTrait<JamsMissilesInfo>
	{
		public WDist Range { get { return IsTraitDisabled ? WDist.Zero : Info.Range; } }
		public Stance DeflectionStances { get { return Info.DeflectionStances; } }
		public int Chance { get { return Info.Chance; } }

		public JamsMissiles(JamsMissilesInfo info)
			: base(info) { }
	}
}
