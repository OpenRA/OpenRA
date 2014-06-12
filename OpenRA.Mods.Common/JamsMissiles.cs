#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class JamsMissilesInfo : ITraitInfo
	{
		public readonly int Range = 0;
		public readonly bool AlliedMissiles = true;
		public readonly int Chance = 100;

		public object Create(ActorInitializer init) { return new JamsMissiles(this); }
	}

	class JamsMissiles
	{
		readonly JamsMissilesInfo info;

		// Convert cells to world units
		public int Range { get { return 1024 * info.Range; } }
		public bool AlliedMissiles { get { return info.AlliedMissiles; } }
		public int Chance { get { return info.Chance; } }

		public JamsMissiles(JamsMissilesInfo info) { this.info = info; }
	}
}