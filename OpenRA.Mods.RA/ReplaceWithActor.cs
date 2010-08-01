#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
	class ReplaceWithActorInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string Actor = null;

		public object Create(ActorInitializer init) { return new ReplaceWithActor(init.self, this); } 
	}

	class ReplaceWithActor
	{
		public ReplaceWithActor(Actor self, ReplaceWithActorInfo info)
		{
			self.World.AddFrameEndTask(w =>
			{
				w.Remove(self);
				w.CreateActor(info.Actor, new TypeDictionary
				{
					new LocationInit( self.Location ),
					new OwnerInit( self.Owner ),
				});
			});
		}
	}
}
