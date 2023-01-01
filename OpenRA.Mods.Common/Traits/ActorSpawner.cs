#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("An actor with this trait indicates a valid spawn point for actors of ActorSpawnManager.")]
	public class ActorSpawnerInfo : ConditionalTraitInfo
	{
		[Desc("Type of ActorSpawner with which it connects.")]
		public readonly HashSet<string> Types = new HashSet<string>() { };

		public override object Create(ActorInitializer init) { return new ActorSpawner(this); }
	}

	public class ActorSpawner : ConditionalTrait<ActorSpawnerInfo>
	{
		public ActorSpawner(ActorSpawnerInfo info)
			: base(info) { }

		public HashSet<string> Types => Info.Types;
	}
}
