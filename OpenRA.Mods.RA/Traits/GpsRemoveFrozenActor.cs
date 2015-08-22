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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Removes frozen actors of actors that are dead or sold," +
		" when having an active GPS power.")]
	public class GpsRemoveFrozenActorInfo : ITraitInfo
	{
		[Desc("Should this trait also affect allied players?")]
		public bool GrantAllies = true;

		public object Create(ActorInitializer init) { return new GpsRemoveFrozenActor(init.Self, this); }
	}

	public class GpsRemoveFrozenActor : IRemoveFrozenActor
	{
		readonly GpsWatcher[] watchers;
		readonly GpsRemoveFrozenActorInfo info;

		public GpsRemoveFrozenActor(Actor self, GpsRemoveFrozenActorInfo info)
		{
			this.info = info;
			watchers = self.World.ActorsWithTrait<GpsWatcher>().Select(w => w.Trait).ToArray();
		}

		public bool RemoveActor(Actor self, Player owner)
		{
			if (!self.IsDead)
				return false;

			foreach (var w in watchers)
			{
				if (w.Owner != owner && !(info.GrantAllies && w.Owner.IsAlliedWith(owner)))
					continue;

				if (w.Launched)
					return true;
			}

			return false;
		}
	}
}
