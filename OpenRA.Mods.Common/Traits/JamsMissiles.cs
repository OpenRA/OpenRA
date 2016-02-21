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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class JamsMissilesInfo : ITraitInfo
	{
		public readonly WDist Range = WDist.Zero;
		public readonly bool AlliedMissiles = true;
		public readonly int Chance = 100;

		public object Create(ActorInitializer init) { return new JamsMissiles(this); }
	}

	public class JamsMissiles
	{
		readonly JamsMissilesInfo info;

		public WDist Range { get { return info.Range; } }
		public bool AlliedMissiles { get { return info.AlliedMissiles; } }
		public int Chance { get { return info.Chance; } }

		public JamsMissiles(JamsMissilesInfo info) { this.info = info; }
	}
}
