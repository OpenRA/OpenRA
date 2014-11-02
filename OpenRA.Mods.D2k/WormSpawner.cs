#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	[Desc("An actor with this trait indicates a valid spawn point for sandworms.")]
	class WormSpawnerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new WormSpawner(); }
	}

	class WormSpawner
	{
	}
}
