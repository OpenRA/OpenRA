#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class FreeActorInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string Actor = null;
		public readonly string InitialActivity = null;
		public readonly int2 SpawnOffset = int2.Zero;
		public readonly int Facing = 0;
		
		public object Create( ActorInitializer init ) { return new FreeActor(init.self, this); }
	}

	public class FreeActor
	{
		public FreeActor(Actor self, FreeActorInfo info)
		{			
			self.World.AddFrameEndTask(
				w =>
				{
					var a = w.CreateActor(info.Actor, new TypeDictionary
				    {
						new LocationInit( self.Location + info.SpawnOffset ),
						new OwnerInit( self.Owner ),
						new FacingInit( info.Facing ),
					});

					if (info.InitialActivity != null)
						a.QueueActivity(Game.CreateObject<IActivity>(info.InitialActivity));
				});
		}
	}
}
